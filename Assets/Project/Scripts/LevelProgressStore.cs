using UnityEngine;

// Purpose: Saves and loads the player's current level and highest unlocked level using PlayerPrefs.
public static class LevelProgressStore
{
    private const string CurrentLevelKey = "PrismShift.CurrentLevel";
    private const string HighestUnlockedLevelKey = "PrismShift.HighestUnlockedLevel";

    public static int LoadCurrentLevelIndex(int fallbackIndex)
    {
        return PlayerPrefs.GetInt(CurrentLevelKey, fallbackIndex);
    }

    public static int LoadHighestUnlockedLevelIndex(int fallbackIndex)
    {
        return PlayerPrefs.GetInt(HighestUnlockedLevelKey, fallbackIndex);
    }

    public static void SaveCurrentLevelIndex(int levelIndex)
    {
        PlayerPrefs.SetInt(CurrentLevelKey, levelIndex);
        PlayerPrefs.Save();
    }

    public static void SaveProgress(int currentLevelIndex, int highestUnlockedLevelIndex)
    {
        PlayerPrefs.SetInt(CurrentLevelKey, currentLevelIndex);
        PlayerPrefs.SetInt(HighestUnlockedLevelKey, highestUnlockedLevelIndex);
        PlayerPrefs.Save();
    }
}
