using System.Collections.Generic;
using UnityEngine;

// Purpose: Temporarily carries copied campaign data from level selection into the gameplay scene.
/// <summary>
/// Carries copied campaign data from SelectionScene to PrismShift for the current
/// play session. The ScriptableObject stays in SelectionScene.
/// </summary>
public static class GameSession
{
    private static LevelEntry[] campaignLevels;
    private static int selectedLevelIndex = -1;

    public static bool SetCampaign(IList<LevelEntry> sourceLevels, int levelIndex)
    {
        if (sourceLevels == null || sourceLevels.Count == 0 ||
            levelIndex < 0 || levelIndex >= sourceLevels.Count)
        {
            ClearCampaign();
            return false;
        }

        LevelEntry[] copiedLevels = new LevelEntry[sourceLevels.Count];
        for (int i = 0; i < sourceLevels.Count; i++)
        {
            if (sourceLevels[i] == null)
            {
                ClearCampaign();
                return false;
            }

            copiedLevels[i] = CopyLevelEntry(sourceLevels[i]);
        }

        campaignLevels = copiedLevels;
        selectedLevelIndex = levelIndex;
        return true;
    }

    public static bool TryConsumeCampaign(out LevelEntry[] levels, out int levelIndex)
    {
        if (campaignLevels == null || campaignLevels.Length == 0 ||
            selectedLevelIndex < 0 || selectedLevelIndex >= campaignLevels.Length)
        {
            levels = null;
            levelIndex = -1;
            ClearCampaign();
            return false;
        }

        levels = campaignLevels;
        levelIndex = selectedLevelIndex;
        ClearCampaign();
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForNewPlaySession()
    {
        ClearCampaign();
    }

    private static void ClearCampaign()
    {
        campaignLevels = null;
        selectedLevelIndex = -1;
    }

    private static LevelEntry CopyLevelEntry(LevelEntry source)
    {
        return new LevelEntry
        {
            levelName = source.levelName,
            levelTexture = source.levelTexture,
            targetBoardAreaSize = source.targetBoardAreaSize,
            spacing = source.spacing,
            boardOffset = source.boardOffset,
            socketScaleFactor = source.socketScaleFactor,
            manualSlotSize = source.manualSlotSize,
            manualSocketSize = source.manualSocketSize
        };
    }
}
