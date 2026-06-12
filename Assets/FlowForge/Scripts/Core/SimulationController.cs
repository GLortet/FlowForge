using System.Collections.Generic;
using FlowForge.Data;
using FlowForge.Lean;
using FlowForge.Simulation;
using UnityEngine;

namespace FlowForge.Core
{
    /// <summary>
    /// Orchestrates order release, routing and runtime balancing for the first playable prototype.
    /// </summary>
    public class SimulationController : MonoBehaviour
    {
        [SerializeField] private LevelConfig levelConfig;
        [SerializeField] private WorkshopGrid workshopGrid;
        [SerializeField] private LeanImprovementSystem improvementSystem;

        private readonly List<Queue<ProductionOrder>> stationQueues = new List<Queue<ProductionOrder>>();
        private readonly List<Workstation> workstations = new List<Workstation>();
        private readonly List<OperatorAgent> operators = new List<OperatorAgent>();
        private readonly MetricsTracker metrics = new MetricsTracker();
        private ScoreSystem scoreSystem;
        private int nextOrderId;
        private float releaseTimer;

        public MetricsTracker Metrics => metrics;
        public ScoreSystem Score => scoreSystem;
        public IReadOnlyList<Workstation> Workstations => workstations;
        public IReadOnlyList<OperatorAgent> Operators => operators;
        public LeanImprovementSystem ImprovementSystem => improvementSystem;

        public void StartPrototype(LevelConfig config = null)
        {
            levelConfig = config;
            if (improvementSystem == null)
            {
                improvementSystem = GetComponent<LeanImprovementSystem>() ?? gameObject.AddComponent<LeanImprovementSystem>();
            }

            scoreSystem = new ScoreSystem(metrics);
            BuildWorld();
            metrics.StartRun(workstations, operators);
            improvementSystem.Initialize(workstations, operators, metrics);
        }

        private void Update()
        {
            if (workstations.Count == 0)
            {
                return;
            }

            ReleaseOrdersOverTime();
            FeedWaitingStations();
            metrics.RecordQueue(GetTotalQueueSize());
            AnimateOperatorToBottleneck();
        }

        private void BuildWorld()
        {
            if (workshopGrid == null)
            {
                workshopGrid = new GameObject("Workshop Grid").AddComponent<WorkshopGrid>();
            }

            var gridWidth = levelConfig != null ? levelConfig.gridWidth : 8;
            var gridHeight = levelConfig != null ? levelConfig.gridHeight : 6;
            workshopGrid.Build(gridWidth, gridHeight, new Color(0.82f, 0.78f, 0.68f));

            SpawnWorkstations();
            SpawnOperators();
        }

        private void SpawnWorkstations()
        {
            workstations.Clear();
            stationQueues.Clear();
            var configs = GetMachineSequence();
            for (var i = 0; i < configs.Count; i++)
            {
                var machine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                machine.name = configs[i].displayName;
                machine.transform.position = workshopGrid.GridToWorld(2 + i * 2, 3) + Vector3.up * 0.45f;
                machine.transform.localScale = new Vector3(1.1f, 0.9f, 1.1f);

                var station = machine.AddComponent<Workstation>();
                station.Initialize(configs[i], configs[i].color);
                station.OrderProcessed += HandleOrderProcessed;
                station.BrokeDown += station => Debug.Log($"Andon: {station.name} needs help.");
                station.Repaired += station => Debug.Log($"TPM: {station.name} is stable again.");
                workstations.Add(station);
                stationQueues.Add(new Queue<ProductionOrder>());
            }
        }

        private void SpawnOperators()
        {
            operators.Clear();
            for (var i = 0; i < 2; i++)
            {
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = $"Operator {i + 1}";
                capsule.transform.position = workshopGrid.GridToWorld(1, 1 + i) + Vector3.up * 0.5f;
                capsule.transform.localScale = Vector3.one * 0.55f;

                var agent = capsule.AddComponent<OperatorAgent>();
                agent.Initialize(new Color(0.25f, 0.9f, 1f));
                operators.Add(agent);
            }
        }

        private void ReleaseOrdersOverTime()
        {
            var orderLimit = levelConfig != null ? levelConfig.customerOrders : 30;
            if (nextOrderId >= orderLimit)
            {
                return;
            }

            releaseTimer += Time.deltaTime;
            if (releaseTimer < 1.4f)
            {
                return;
            }

            releaseTimer = 0f;
            var order = new ProductionOrder(nextOrderId++, Time.time);
            stationQueues[0].Enqueue(order);
            metrics.RecordOrderStarted();
        }

        private void FeedWaitingStations()
        {
            for (var i = 0; i < workstations.Count; i++)
            {
                if (stationQueues[i].Count == 0 || workstations[i].IsBusy || workstations[i].IsBroken)
                {
                    continue;
                }

                workstations[i].TryStart(stationQueues[i].Dequeue());
            }
        }

        private void HandleOrderProcessed(Workstation station, ProductionOrder order, bool defective)
        {
            if (defective)
            {
                metrics.RecordDefect(station.Config);
            }

            order.CurrentStep++;
            if (order.CurrentStep >= workstations.Count)
            {
                order.Complete(Time.time);
                metrics.RecordCompleted(order);
                return;
            }

            var nextStation = workstations[order.CurrentStep];
            if (!nextStation.TryStart(order))
            {
                stationQueues[order.CurrentStep].Enqueue(order);
            }
        }

        private int GetTotalQueueSize()
        {
            var total = 0;
            foreach (var queue in stationQueues)
            {
                total += queue.Count;
            }

            return total;
        }

        private void AnimateOperatorToBottleneck()
        {
            if (operators.Count == 0 || workstations.Count == 0)
            {
                return;
            }

            for (var i = 0; i < operators.Count; i++)
            {
                var target = workstations[(Time.frameCount / 120 + i) % workstations.Count];
                operators[i].MoveTo(target.transform.position + new Vector3(0.6f, 0f, -0.6f), target.EffectiveTravelDistance * 0.0008f);
            }
        }

        private List<MachineConfig> GetMachineSequence()
        {
            if (levelConfig != null && levelConfig.machineSequence.Count > 0)
            {
                return levelConfig.machineSequence;
            }

            return new List<MachineConfig>
            {
                CreateFallbackMachine("prep", "Préparation composants", new Color(0.35f, 0.62f, 1f), 2.8f, 0.06f),
                CreateFallbackMachine("assembly", "Assemblage mouvement", new Color(0.95f, 0.65f, 0.22f), 4.2f, 0.11f),
                CreateFallbackMachine("quality", "Contrôle qualité", new Color(0.5f, 0.9f, 0.55f), 3.3f, 0.04f)
            };
        }

        private static MachineConfig CreateFallbackMachine(string id, string displayName, Color color, float processTime, float defectChance)
        {
            var config = ScriptableObject.CreateInstance<MachineConfig>();
            config.id = id;
            config.displayName = displayName;
            config.color = color;
            config.processTimeSeconds = processTime;
            config.defectChance = defectChance;
            config.breakdownChancePerCycle = 0.015f;
            config.repairTimeSeconds = 4f;
            config.operatorTravelDistance = 2.5f;
            return config;
        }
    }
}
