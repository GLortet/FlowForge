using System.Collections.Generic;
using System.Linq;
using FlowForge.Data;
using UnityEngine;

namespace FlowForge.Simulation
{
    /// <summary>
    /// Centralizes Lean indicators so UI, scoring and missions read the same definitions.
    /// </summary>
    public class MetricsTracker
    {
        private readonly List<float> completedLeadTimes = new List<float>();
        private readonly List<Workstation> workstations = new List<Workstation>();
        private readonly List<OperatorAgent> operators = new List<OperatorAgent>();
        private float simulationStart;

        public int StartedOrders { get; private set; }
        public int CompletedOrders { get; private set; }
        public int ScrapCount { get; private set; }
        public int ReworkCount { get; private set; }
        public int WaitingQueueSize { get; private set; }
        public float NonQualityCost { get; private set; }
        public float WaitingTime { get; private set; }
        public float Money { get; private set; } = 500f;
        public float Reputation { get; private set; }
        public float LeanScoreBonus { get; private set; }

        public void StartRun(IEnumerable<Workstation> stations, IEnumerable<OperatorAgent> agents)
        {
            workstations.Clear();
            workstations.AddRange(stations);
            operators.Clear();
            operators.AddRange(agents);
            simulationStart = Time.time;
            StartedOrders = 0;
            CompletedOrders = 0;
            ScrapCount = 0;
            ReworkCount = 0;
            WaitingQueueSize = 0;
            NonQualityCost = 0f;
            WaitingTime = 0f;
            LeanScoreBonus = 0f;
            completedLeadTimes.Clear();
        }

        public void RecordOrderStarted()
        {
            StartedOrders++;
        }

        public void RecordQueue(int queueSize)
        {
            WaitingQueueSize = queueSize;
            WaitingTime += queueSize * Time.deltaTime;
        }

        public void RecordDefect(MachineConfig config)
        {
            ScrapCount++;
            ReworkCount++;
            NonQualityCost += config != null ? config.nonQualityCost : 25f;
            Money -= config != null ? config.nonQualityCost : 25f;
        }

        public void RecordCompleted(ProductionOrder order)
        {
            CompletedOrders++;
            completedLeadTimes.Add(order.LeadTime);
            Money += order.QualityState == ItemQualityState.Reworked ? 35f : 50f;
            Reputation += order.QualityState == ItemQualityState.Defective ? 0f : 0.35f;
        }

        public void AddImprovementReward(float reputation, float money, float leanScoreBonus = 0f)
        {
            Reputation += reputation;
            Money += money;
            LeanScoreBonus += leanScoreBonus;
        }

        public void ApplyVisibleWasteReduction(float scrapReductionPercent, float nonQualityCostReductionPercent)
        {
            var clampedScrapReduction = Mathf.Clamp01(scrapReductionPercent);
            if (ScrapCount > 0)
            {
                ScrapCount = Mathf.Max(0, Mathf.FloorToInt(ScrapCount * (1f - clampedScrapReduction)));
            }

            if (ReworkCount > 0)
            {
                ReworkCount = Mathf.Max(0, Mathf.FloorToInt(ReworkCount * (1f - clampedScrapReduction)));
            }

            NonQualityCost *= 1f - Mathf.Clamp01(nonQualityCostReductionPercent);
        }

        public float GetMetric(MetricType type)
        {
            switch (type)
            {
                case MetricType.ScrapRate: return ScrapRate * 100f;
                case MetricType.LeadTime: return AverageLeadTime;
                case MetricType.Oee: return Oee * 100f;
                case MetricType.ServiceRate: return ServiceRate * 100f;
                case MetricType.Inventory: return WaitingQueueSize;
                case MetricType.WaitingTime: return WaitingTime;
                case MetricType.TravelDistance: return TravelDistance;
                case MetricType.ReworkCount: return ReworkCount;
                case MetricType.NonQualityCost: return NonQualityCost;
                case MetricType.CustomerSatisfaction: return CustomerSatisfaction * 100f;
                case MetricType.TeamStress: return TeamStress * 100f;
                case MetricType.LeanScore: return LeanScore;
                default: return 0f;
            }
        }

        public float ScrapRate => StartedOrders == 0 ? 0f : (float)ScrapCount / StartedOrders;
        public float AverageLeadTime => completedLeadTimes.Count == 0 ? 0f : completedLeadTimes.Average();
        public float ServiceRate => StartedOrders == 0 ? 1f : Mathf.Clamp01((float)CompletedOrders / StartedOrders);
        public float TravelDistance => operators.Sum(agent => agent.DistanceWalked);
        public float TeamStress => operators.Count == 0 ? 0f : operators.Average(agent => agent.Stress);

        public float Oee
        {
            get
            {
                var elapsed = Mathf.Max(1f, Time.time - simulationStart);
                if (workstations.Count == 0)
                {
                    return 1f;
                }

                var availability = 1f - Mathf.Clamp01(workstations.Sum(station => station.BrokenTime) / (elapsed * workstations.Count));
                var performance = Mathf.Clamp01(workstations.Sum(station => station.BusyTime) / (elapsed * workstations.Count));
                var quality = 1f - ScrapRate;
                return Mathf.Clamp01(availability * performance * quality);
            }
        }

        public float CustomerSatisfaction => Mathf.Clamp01((ServiceRate * 0.55f) + ((1f - ScrapRate) * 0.3f) + ((1f - TeamStress) * 0.15f));

        public float LeanScore
        {
            get
            {
                var flowScore = Mathf.Clamp01(1f - AverageLeadTime / 40f);
                var wasteScore = Mathf.Clamp01(1f - ScrapRate);
                var peopleScore = Mathf.Clamp01(1f - TeamStress);
                var serviceScore = ServiceRate;
                return Mathf.Clamp(Mathf.RoundToInt((flowScore * 25f) + (wasteScore * 30f) + (peopleScore * 20f) + (serviceScore * 25f) + LeanScoreBonus), 0, 100);
            }
        }
    }
}
