using UnityEngine;

namespace FlowForge.Data
{
    /// <summary>
    /// Configures a production resource without hard-coding balancing values in scene objects.
    /// Designers can create one asset per machine family and reuse it across worlds.
    /// </summary>
    [CreateAssetMenu(menuName = "FlowForge/Data/Machine Config", fileName = "MachineConfig")]
    public class MachineConfig : ScriptableObject
    {
        [Header("Identity")]
        public string id = "machine";
        public string displayName = "Machine";
        public Color color = new Color(0.2f, 0.55f, 0.9f);

        [Header("Flow")]
        [Min(0.25f)] public float processTimeSeconds = 3f;
        [Range(0f, 1f)] public float defectChance = 0.08f;
        [Range(0f, 1f)] public float breakdownChancePerCycle = 0.02f;
        [Min(0f)] public float repairTimeSeconds = 5f;

        [Header("Lean Signals")]
        [Min(0f)] public float nonQualityCost = 30f;
        [Min(0f)] public float operatorTravelDistance = 2f;
    }
}
