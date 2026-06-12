using UnityEngine;

namespace FlowForge.Data
{
    /// <summary>
    /// Describes a Lean action that can be unlocked and applied by the player.
    /// Effects are intentionally simple for the prototype and can evolve into strategy objects later.
    /// </summary>
    [CreateAssetMenu(menuName = "FlowForge/Data/Lean Tool Config", fileName = "LeanToolConfig")]
    public class LeanToolConfig : ScriptableObject
    {
        public string id = "5s";
        public string displayName = "5S";
        [TextArea] public string description = "Ranger, nettoyer et standardiser l'atelier.";
        [Min(0f)] public float unlockReputation = 0f;

        [Header("Prototype Effects")]
        [Range(0f, 1f)] public float processTimeReduction = 0.12f;
        [Range(0f, 1f)] public float travelDistanceReduction = 0.35f;
        [Range(0f, 1f)] public float defectReduction = 0.08f;
        [Range(0f, 1f)] public float stressReduction = 0.15f;
    }
}
