using FlowForge.Simulation;
using UnityEngine;

namespace FlowForge.Core
{
    /// <summary>
    /// Converts Lean metrics into readable stars and a final score. Money alone cannot maximize the score.
    /// </summary>
    public class ScoreSystem
    {
        private readonly MetricsTracker metrics;

        public ScoreSystem(MetricsTracker metricsTracker)
        {
            metrics = metricsTracker;
        }

        public int CurrentScore => Mathf.RoundToInt(metrics.LeanScore + metrics.CustomerSatisfaction * 20f + metrics.Oee * 15f);

        public int Stars
        {
            get
            {
                if (metrics.LeanScore >= 88f && metrics.CustomerSatisfaction >= 0.9f)
                {
                    return 3;
                }

                if (metrics.LeanScore >= 70f)
                {
                    return 2;
                }

                return metrics.LeanScore >= 50f ? 1 : 0;
            }
        }
    }
}
