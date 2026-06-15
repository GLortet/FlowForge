using System.Collections.Generic;
using UnityEngine;

namespace FlowForge.Data
{
    /// <summary>
    /// High-level level definition. The prototype can run from fallback values, while production scenes can bind assets.
    /// </summary>
    [CreateAssetMenu(menuName = "FlowForge/Data/Level Config", fileName = "LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        public string id = "watch_level_01";
        public string displayName = "Atelier initial";
        public string worldId = "luxury_watches";
        [TextArea] public string briefing = "A small workshop starts its Lean journey.";
        [Min(3)] public int gridWidth = 8;
        [Min(3)] public int gridHeight = 6;
        [Min(1)] public int customerOrders = 30;
        [Min(60)] public int timeLimitSeconds = 420;
        public List<MachineConfig> machineSequence = new List<MachineConfig>();
        public List<MissionConfig> missions = new List<MissionConfig>();
        public List<LeanToolConfig> unlockedTools = new List<LeanToolConfig>();
    }
}
