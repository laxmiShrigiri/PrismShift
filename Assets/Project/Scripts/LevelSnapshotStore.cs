using System;
using System.Collections.Generic;
using UnityEngine;

// Purpose: Saves and restores each level's board and tray layout so unfinished puzzles can be resumed.
public static class LevelSnapshotStore
{
    private const string KeyPrefix = "PrismShift.LevelSnapshot.";
    public const int CurrentSchemaVersion = 1;

    [Serializable]
    public sealed class Snapshot
    {
        public int schemaVersion = CurrentSchemaVersion;
        public string levelName;
        public string textureName;
        public int gridWidth;
        public int gridHeight;
        public bool isComplete;
        public List<BoardSlotState> boardSlots = new List<BoardSlotState>();
        public List<TraySlotState> traySlots = new List<TraySlotState>();
    }

    [Serializable]
    public sealed class BoardSlotState
    {
        public int x;
        public int y;
        public bool hasTile;
        public Color tileColor;
    }

    [Serializable]
    public sealed class TraySlotState
    {
        public int slotIndex;
        public bool hasTile;
        public Color tileColor;
    }

    public static void Save(int levelIndex, Snapshot snapshot)
    {
        if (snapshot == null || levelIndex < 0)
            return;

        PlayerPrefs.SetString(KeyFor(levelIndex), JsonUtility.ToJson(snapshot));
        PlayerPrefs.Save();
    }

    public static bool TryLoad(int levelIndex, out Snapshot snapshot)
    {
        snapshot = null;
        if (levelIndex < 0)
            return false;

        string json = PlayerPrefs.GetString(KeyFor(levelIndex), string.Empty);
        if (string.IsNullOrEmpty(json))
            return false;

        try
        {
            snapshot = JsonUtility.FromJson<Snapshot>(json);
            return snapshot != null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[LevelSnapshotStore] Could not read saved level {levelIndex + 1}: {exception.Message}");
            return false;
        }
    }

    public static void Clear(int levelIndex)
    {
        if (levelIndex < 0)
            return;

        PlayerPrefs.DeleteKey(KeyFor(levelIndex));
        PlayerPrefs.Save();
    }

    public static void ClearAll(int levelCount)
    {
        for (int i = 0; i < levelCount; i++)
            PlayerPrefs.DeleteKey(KeyFor(i));

        PlayerPrefs.Save();
    }

    private static string KeyFor(int levelIndex)
    {
        return KeyPrefix + levelIndex;
    }
}
