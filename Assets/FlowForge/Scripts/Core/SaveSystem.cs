using System;
using UnityEngine;

namespace FlowForge.Core
{
    [Serializable]
    public class PlayerProgress
    {
        public int money = 500;
        public int reputation;
        public int highestWatchLevelUnlocked = 1;
        public int highestAutomotiveLevelUnlocked;
        public bool unlocked5S = true;
    }

    /// <summary>
    /// Tiny PlayerPrefs save layer suitable for prototype progress. Can be replaced by cloud saves later.
    /// </summary>
    public static class SaveSystem
    {
        private const string SaveKey = "FlowForge.Progress.v1";

        public static PlayerProgress Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return new PlayerProgress();
            }

            try
            {
                return JsonUtility.FromJson<PlayerProgress>(PlayerPrefs.GetString(SaveKey)) ?? new PlayerProgress();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not load FlowForge progress: {exception.Message}");
                return new PlayerProgress();
            }
        }

        public static void Save(PlayerProgress progress)
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(progress));
            PlayerPrefs.Save();
        }
    }
}
