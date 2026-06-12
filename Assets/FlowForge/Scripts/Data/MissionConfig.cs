using UnityEngine;

namespace FlowForge.Data
{
    public enum MetricType
    {
        ScrapRate,
        LeadTime,
        Oee,
        ServiceRate,
        Inventory,
        WaitingTime,
        TravelDistance,
        ReworkCount,
        NonQualityCost,
        CustomerSatisfaction,
        TeamStress,
        LeanScore
    }

    /// <summary>
    /// Objective data used by levels, daily challenges and tutorials.
    /// </summary>
    [CreateAssetMenu(menuName = "FlowForge/Data/Mission Config", fileName = "MissionConfig")]
    public class MissionConfig : ScriptableObject
    {
        public string id = "mission";
        public string title = "Reduce waste";
        [TextArea] public string briefing = "Observe the flow, identify waste, then improve the process.";
        public MetricType targetMetric = MetricType.LeanScore;
        public bool lowerIsBetter = false;
        public float targetValue = 80f;
        [Min(1)] public int rewardMoney = 100;
        [Min(0)] public int rewardReputation = 5;
    }
}
