using UnityEngine;

namespace FlowForge.Simulation
{
    public enum ItemQualityState
    {
        Good,
        Defective,
        Reworked
    }

    /// <summary>
    /// Runtime unit of customer demand. Keeps timestamps for lead-time and service calculations.
    /// </summary>
    public class ProductionOrder
    {
        public int Id { get; }
        public float CreatedAt { get; }
        public float? CompletedAt { get; private set; }
        public int CurrentStep { get; set; }
        public ItemQualityState QualityState { get; set; } = ItemQualityState.Good;

        public bool IsComplete => CompletedAt.HasValue;
        public float LeadTime => (CompletedAt ?? Time.time) - CreatedAt;

        public ProductionOrder(int id, float createdAt)
        {
            Id = id;
            CreatedAt = createdAt;
        }

        public void Complete(float completedAt)
        {
            CompletedAt = completedAt;
        }
    }
}
