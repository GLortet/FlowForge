using System.Collections.Generic;
using UnityEngine;

namespace FlowForge.Data
{
    /// <summary>
    /// Groups levels and visual direction so new worlds can be added without changing simulation code.
    /// </summary>
    [CreateAssetMenu(menuName = "FlowForge/Data/World Config", fileName = "WorldConfig")]
    public class WorldConfig : ScriptableObject
    {
        public string id = "luxury_watches";
        public string displayName = "Atelier de montres de luxe";
        [TextArea] public string fantasy = "Precision, calm, prestige and uncompromising quality.";
        public Color floorColor = new Color(0.82f, 0.78f, 0.68f);
        public Color accentColor = new Color(0.95f, 0.72f, 0.28f);
        public List<LevelConfig> levels = new List<LevelConfig>();
    }
}
