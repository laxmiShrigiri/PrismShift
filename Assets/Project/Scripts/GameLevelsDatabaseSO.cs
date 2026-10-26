using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelEntry
{
    [Header("Level Info")]
    public string levelName = "Level 1";

    [Header("Texture")]
    [Tooltip("The pixel grid PNG. Ensure Read/Write is enabled!")]
    public Texture2D levelTexture;

    [Header("Board Frame & Sizing")]
    public Vector2 targetBoardAreaSize = new Vector2(850f, 850f);
    public Vector2 spacing = new Vector2(6f, 6f);
    public Vector2 boardOffset = new Vector2(0f, 70f);

    [Range(0.5f, 1f)]
    public float socketScaleFactor = 0.84f;

    [Header("Manual Sizing Override (Optional)")]
    public Vector2 manualSlotSize = Vector2.zero;
    public Vector2 manualSocketSize = Vector2.zero;
}
[CreateAssetMenu(fileName = "GameLevelsDatabaseSO", menuName = "Scriptable Objects/GameLevelsDatabaseSO")]
public class GameLevelsDatabaseSO : ScriptableObject
{
     [SerializeField] private List<LevelEntry> levels = new List<LevelEntry>();

    public List<LevelEntry> Levels => levels;
    public int TotalLevels => levels != null ? levels.Count : 0;

    public LevelEntry GetLevel(int index)
    {
        if (levels == null || index < 0 || index >= levels.Count)
            return null;

        return levels[index];
    }

    public bool Validate(out string error)
    {
        if (levels == null || levels.Count == 0)
        {
            error = "The level database is empty.";
            return false;
        }

        for (int i = 0; i < levels.Count; i++)
        {
            LevelEntry level = levels[i];
            if (level == null)
            {
                error = $"Level {i + 1} is missing.";
                return false;
            }

            if (level.levelTexture == null)
            {
                error = $"Level '{level.levelName}' has no texture assigned.";
                return false;
            }

            if (!level.levelTexture.isReadable)
            {
                error = $"Level '{level.levelName}' texture must have Read/Write enabled.";
                return false;
            }

            if (level.targetBoardAreaSize.x <= 0f || level.targetBoardAreaSize.y <= 0f)
            {
                error = $"Level '{level.levelName}' has an invalid board area size.";
                return false;
            }

            if (level.socketScaleFactor <= 0f || level.socketScaleFactor > 1f)
            {
                error = $"Level '{level.levelName}' has an invalid socket scale factor.";
                return false;
            }

            if (level.spacing.x < 0f || level.spacing.y < 0f)
            {
                error = $"Level '{level.levelName}' cannot use negative spacing.";
                return false;
            }

            if (level.manualSlotSize != Vector2.zero &&
                (level.manualSlotSize.x <= 0f || level.manualSlotSize.y <= 0f ||
                 level.manualSocketSize.x <= 0f || level.manualSocketSize.y <= 0f))
            {
                error = $"Level '{level.levelName}' must define positive slot and socket sizes when using manual sizing.";
                return false;
            }

            if (level.manualSlotSize == Vector2.zero &&
                (level.targetBoardAreaSize.x - (level.levelTexture.width - 1) * level.spacing.x <= 0f ||
                 level.targetBoardAreaSize.y - (level.levelTexture.height - 1) * level.spacing.y <= 0f))
            {
                error = $"Level '{level.levelName}' spacing leaves no room for its cells.";
                return false;
            }

            bool hasActiveCell = false;
            Color[] pixels = level.levelTexture.GetPixels();
            for (int pixelIndex = 0; pixelIndex < pixels.Length; pixelIndex++)
            {
                if (pixels[pixelIndex].a >= 0.1f)
                {
                    hasActiveCell = true;
                    break;
                }
            }

            if (!hasActiveCell)
            {
                error = $"Level '{level.levelName}' has no active cells.";
                return false;
            }
        }

        error = null;
        return true;
    }
}
