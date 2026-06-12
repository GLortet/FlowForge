using System;
using System.Collections.Generic;
using FlowForge.Data;
using FlowForge.Simulation;
using UnityEngine;

namespace FlowForge.Lean
{
    /// <summary>
    /// Applies Lean tools to the simulation. For the prototype, 5S improves flow, motion, defects and stress.
    /// </summary>
    public class LeanImprovementSystem : MonoBehaviour
    {
        [SerializeField] private LeanToolConfig fiveSConfig;
        [SerializeField] private ParticleSystem celebrationPrefab;

        private readonly HashSet<string> appliedToolIds = new HashSet<string>();
        private IReadOnlyList<Workstation> workstations;
        private IReadOnlyList<OperatorAgent> operators;
        private MetricsTracker metrics;

        public bool HasApplied5S => appliedToolIds.Contains(GetFiveSConfig().id);
        public event Action<LeanToolConfig> ToolApplied;

        public void Initialize(IReadOnlyList<Workstation> stations, IReadOnlyList<OperatorAgent> agents, MetricsTracker tracker)
        {
            workstations = stations;
            operators = agents;
            metrics = tracker;
        }

        public void Apply5S()
        {
            var config = GetFiveSConfig();
            if (appliedToolIds.Contains(config.id))
            {
                return;
            }

            appliedToolIds.Add(config.id);

            foreach (var station in workstations)
            {
                station.ApplyLeanMultiplier(config.processTimeReduction, config.defectReduction, config.travelDistanceReduction);
                SpawnCelebration(station.transform.position + Vector3.up * 1.2f);
            }

            foreach (var agent in operators)
            {
                agent.ReduceStress(config.stressReduction);
            }

            metrics?.AddImprovementReward(3f, -75f);
            ToolApplied?.Invoke(config);
        }

        private LeanToolConfig GetFiveSConfig()
        {
            if (fiveSConfig != null)
            {
                return fiveSConfig;
            }

            fiveSConfig = ScriptableObject.CreateInstance<LeanToolConfig>();
            fiveSConfig.id = "5s";
            fiveSConfig.displayName = "5S";
            fiveSConfig.description = "Nettoie, ordonne et standardise les postes pour réduire mouvements inutiles et erreurs.";
            fiveSConfig.processTimeReduction = 0.12f;
            fiveSConfig.travelDistanceReduction = 0.35f;
            fiveSConfig.defectReduction = 0.08f;
            fiveSConfig.stressReduction = 0.15f;
            return fiveSConfig;
        }

        private void SpawnCelebration(Vector3 position)
        {
            if (celebrationPrefab != null)
            {
                Instantiate(celebrationPrefab, position, Quaternion.identity);
                return;
            }

            var sparkle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sparkle.name = "5S Feedback Sparkle";
            sparkle.transform.position = position;
            sparkle.transform.localScale = Vector3.one * 0.25f;
            var renderer = sparkle.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = new Color(1f, 0.84f, 0.25f);
            Destroy(sparkle, 1.2f);
        }
    }
}
