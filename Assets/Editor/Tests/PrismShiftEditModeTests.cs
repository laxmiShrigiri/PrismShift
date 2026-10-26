using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Purpose: Covers the puzzle's key data and board-search rules without launching a game scene.
public sealed class PrismShiftEditModeTests
{
    private const int SnapshotTestLevelIndex = 987654;
    private const string CurrentLevelKey = "PrismShift.CurrentLevel";
    private const string HighestUnlockedLevelKey = "PrismShift.HighestUnlockedLevel";
    private const string SnapshotKey = "PrismShift.LevelSnapshot.987654";

    private readonly List<GameObject> createdObjects = new List<GameObject>();
    private bool hadCurrentLevel;
    private bool hadHighestUnlockedLevel;
    private bool hadSnapshot;
    private int previousCurrentLevel;
    private int previousHighestUnlockedLevel;
    private string previousSnapshot;

    [SetUp]
    public void SetUp()
    {
        GameSession.TryConsumeCampaign(out _, out _);

        hadCurrentLevel = PlayerPrefs.HasKey(CurrentLevelKey);
        previousCurrentLevel = PlayerPrefs.GetInt(CurrentLevelKey);
        hadHighestUnlockedLevel = PlayerPrefs.HasKey(HighestUnlockedLevelKey);
        previousHighestUnlockedLevel = PlayerPrefs.GetInt(HighestUnlockedLevelKey);
        hadSnapshot = PlayerPrefs.HasKey(SnapshotKey);
        previousSnapshot = PlayerPrefs.GetString(SnapshotKey);
    }

    [TearDown]
    public void TearDown()
    {
        GameSession.TryConsumeCampaign(out _, out _);

        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }
        createdObjects.Clear();

        RestoreIntPreference(CurrentLevelKey, hadCurrentLevel, previousCurrentLevel);
        RestoreIntPreference(HighestUnlockedLevelKey, hadHighestUnlockedLevel, previousHighestUnlockedLevel);
        if (hadSnapshot)
            PlayerPrefs.SetString(SnapshotKey, previousSnapshot);
        else
            PlayerPrefs.DeleteKey(SnapshotKey);
        PlayerPrefs.Save();
    }

    [TestCase(0.04f, true)]
    [TestCase(0.06f, false)]
    public void ColorsMatch_UsesExpectedRgbTolerance(float redDifference, bool expectedToMatch)
    {
        Color first = new Color(0.5f, 0.5f, 0.5f, 1f);
        Color second = new Color(0.5f + redDifference, 0.5f, 0.5f, 0.2f);

        Assert.AreEqual(expectedToMatch, BoardSlot.ColorsMatch(first, second));
    }

    [Test]
    public void FindConnectedUnsolvedColorGroup_IncludesDiagonalsAndExcludesOtherGroups()
    {
        BoardSlot[,] grid = CreateGrid(4, 3, Color.blue);
        BoardSlot first = grid[0, 0];
        BoardSlot diagonal = grid[1, 1];
        BoardSlot differentColor = grid[0, 1];
        BoardSlot alreadySolved = grid[1, 0];
        BoardSlot separated = grid[3, 2];

        AddTile(first, Color.red);
        AddTile(diagonal, Color.red);
        AddTile(differentColor, Color.green);
        AddTile(alreadySolved, Color.blue);
        AddTile(separated, Color.red);

        List<BoardSlot> group = PuzzleBoardSearch.FindConnectedUnsolvedColorGroup(grid, first);

        Assert.AreEqual(2, group.Count);
        CollectionAssert.Contains(group, first);
        CollectionAssert.Contains(group, diagonal);
        CollectionAssert.DoesNotContain(group, differentColor);
        CollectionAssert.DoesNotContain(group, alreadySolved);
        CollectionAssert.DoesNotContain(group, separated);
    }

    [Test]
    public void FindConnectedEmptyTargetSlots_FindsOnlyAdjacentMatchingSockets()
    {
        BoardSlot[,] grid = CreateGrid(4, 3, Color.blue);
        BoardSlot first = grid[0, 0];
        BoardSlot diagonal = grid[1, 1];
        BoardSlot separated = grid[3, 2];
        first.TargetFloorColor = Color.red;
        diagonal.TargetFloorColor = Color.red;
        separated.TargetFloorColor = Color.red;

        List<BoardSlot> group = PuzzleBoardSearch.FindConnectedEmptyTargetSlots(grid, first, Color.red);

        Assert.AreEqual(2, group.Count);
        CollectionAssert.Contains(group, first);
        CollectionAssert.Contains(group, diagonal);
        CollectionAssert.DoesNotContain(group, separated);
    }

    [Test]
    public void GameSession_CopiesCampaignAndAllowsItToBeConsumedOnlyOnce()
    {
        LevelEntry first = new LevelEntry { levelName = "First", spacing = new Vector2(4f, 5f) };
        LevelEntry second = new LevelEntry { levelName = "Second", boardOffset = new Vector2(7f, 8f) };
        List<LevelEntry> source = new List<LevelEntry> { first, second };

        Assert.IsTrue(GameSession.SetCampaign(source, 1));
        first.levelName = "Changed after selection";
        second.boardOffset = Vector2.zero;

        Assert.IsTrue(GameSession.TryConsumeCampaign(out LevelEntry[] copiedCampaign, out int selectedIndex));
        Assert.AreEqual(1, selectedIndex);
        Assert.AreEqual(2, copiedCampaign.Length);
        Assert.AreEqual("First", copiedCampaign[0].levelName);
        Assert.AreEqual(new Vector2(4f, 5f), copiedCampaign[0].spacing);
        Assert.AreEqual(new Vector2(7f, 8f), copiedCampaign[1].boardOffset);
        Assert.AreNotSame(first, copiedCampaign[0]);
        Assert.IsFalse(GameSession.TryConsumeCampaign(out _, out _));
    }

    [Test]
    public void LevelProgressStore_SavesCurrentAndUnlockedLevels()
    {
        LevelProgressStore.SaveProgress(3, 5);

        Assert.AreEqual(3, LevelProgressStore.LoadCurrentLevelIndex(-1));
        Assert.AreEqual(5, LevelProgressStore.LoadHighestUnlockedLevelIndex(-1));
    }

    [Test]
    public void LevelSnapshotStore_RoundTripsPartialBoardAndTrayState()
    {
        LevelSnapshotStore.Snapshot snapshot = new LevelSnapshotStore.Snapshot
        {
            levelName = "Test Level",
            textureName = "Test Texture",
            gridWidth = 2,
            gridHeight = 2,
            isComplete = false
        };
        snapshot.boardSlots.Add(new LevelSnapshotStore.BoardSlotState
        {
            x = 1,
            y = 0,
            hasTile = true,
            tileColor = Color.magenta
        });
        snapshot.traySlots.Add(new LevelSnapshotStore.TraySlotState
        {
            slotIndex = 0,
            hasTile = true,
            tileColor = Color.cyan
        });

        LevelSnapshotStore.Save(SnapshotTestLevelIndex, snapshot);

        Assert.IsTrue(LevelSnapshotStore.TryLoad(SnapshotTestLevelIndex, out LevelSnapshotStore.Snapshot restored));
        Assert.AreEqual("Test Level", restored.levelName);
        Assert.AreEqual(2, restored.gridWidth);
        Assert.IsFalse(restored.isComplete);
        Assert.AreEqual(1, restored.boardSlots.Count);
        Assert.AreEqual(1, restored.boardSlots[0].x);
        Assert.IsTrue(restored.boardSlots[0].hasTile);
        Assert.AreEqual(Color.magenta, restored.boardSlots[0].tileColor);
        Assert.AreEqual(1, restored.traySlots.Count);
        Assert.AreEqual(0, restored.traySlots[0].slotIndex);
        Assert.AreEqual(Color.cyan, restored.traySlots[0].tileColor);
    }

    private BoardSlot[,] CreateGrid(int width, int height, Color targetColor)
    {
        BoardSlot[,] grid = new BoardSlot[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject slotObject = new GameObject($"Test Slot {x},{y}");
                createdObjects.Add(slotObject);
                BoardSlot slot = slotObject.AddComponent<BoardSlot>();
                slot.GridCoords = new Vector2Int(x, y);
                slot.TargetFloorColor = targetColor;
                grid[x, y] = slot;
            }
        }

        return grid;
    }

    private void AddTile(BoardSlot slot, Color color)
    {
        GameObject tileObject = new GameObject("Test Tile", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        createdObjects.Add(tileObject);
        Tile tile = tileObject.AddComponent<Tile>();
        tile.SetColor(color);
        slot.CurrentTile = tile;
    }

    private static void RestoreIntPreference(string key, bool existed, int previousValue)
    {
        if (existed)
            PlayerPrefs.SetInt(key, previousValue);
        else
            PlayerPrefs.DeleteKey(key);
    }
}
