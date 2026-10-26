using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Purpose: Builds and controls the current puzzle, saves its state, checks wins, and advances levels.
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private int currentLevelIndex;
    private int totalLevelCount;
    private LevelEntry[] campaignLevels;
    private LevelEntry selectedLevelData;
    private const float CompletionPopupDuration = 1.6f;

    [Header("Prefabs & Containers")]
    [SerializeField] private BoardSlot slotPrefab;
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private RectTransform boardContainer;

    public LevelEntry CurrentLevel { get; private set; }
    public int CurrentLevelIndex => currentLevelIndex;
    public int LevelCount => totalLevelCount;
    public bool IsLevelComplete => isTransitioning;
    public RectTransform BoardContainer => boardContainer;
    public BoardSlot[,] Grid { get; private set; }
    public int GridWidth { get; private set; }
    public int GridHeight { get; private set; }
    public List<BoardSlot> AllActiveSlots { get; private set; } = new List<BoardSlot>();
    public List<Tile> CurrentlyPickedTiles { get; private set; } = new List<Tile>();

    public event System.Action<LevelEntry, int, int> LevelLoaded;
    public event System.Action<bool, bool> LevelCompleted;

    private Vector2 activeSlotSize;
    private Vector2 activeSocketSize;
    private bool isTransitioning = false;
    private int highestUnlockedLevelIndex;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[LevelManager] Only one LevelManager can be active in a scene.");
            enabled = false;
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (!GameSession.TryConsumeCampaign(out campaignLevels, out currentLevelIndex))
        {
            Debug.LogError("[LevelManager] No level was selected. Start from Home and choose a level.");
            return;
        }

        totalLevelCount = campaignLevels != null ? campaignLevels.Length : 0;
        if (totalLevelCount <= 0 ||
            currentLevelIndex < 0 || currentLevelIndex >= totalLevelCount)
        {
            Debug.LogError("[LevelManager] The copied level campaign is invalid.");
            return;
        }

        selectedLevelData = campaignLevels[currentLevelIndex];
        if (selectedLevelData == null)
        {
            Debug.LogError("[LevelManager] The selected level data is invalid.");
            return;
        }

        if (selectedLevelData.levelTexture == null || !selectedLevelData.levelTexture.isReadable)
        {
            Debug.LogError("[LevelManager] The selected level texture is missing or unreadable.");
            return;
        }

        highestUnlockedLevelIndex = Mathf.Clamp(
            LevelProgressStore.LoadHighestUnlockedLevelIndex(0),
            0,
            totalLevelCount - 1);
        if (currentLevelIndex > highestUnlockedLevelIndex)
        {
            Debug.LogError("[LevelManager] The selected level is still locked.");
            return;
        }

        if (slotPrefab == null || tilePrefab == null || boardContainer == null ||
            TrayManager.Instance == null || !TrayManager.Instance.IsConfigured)
        {
            Debug.LogError("[LevelManager] Assign the slot, tile, tray prefabs, and board/tray containers.");
            return;
        }

        LevelProgressStore.SaveCurrentLevelIndex(currentLevelIndex);

        LoadCurrentLevel();
    }

    public void LoadCurrentLevel()
    {
        if (selectedLevelData == null || totalLevelCount == 0)
        {
            Debug.LogError("[LevelManager] No selected level data is available.");
            return;
        }

        if (currentLevelIndex < 0 || currentLevelIndex >= totalLevelCount)
        {
            Debug.LogError($"[LevelManager] Level index {currentLevelIndex} is outside the database.");
            return;
        }

        isTransitioning = false;
        if (BuildLevelFromEntry(selectedLevelData))
            LevelProgressStore.SaveCurrentLevelIndex(currentLevelIndex);
    }

    public bool BuildLevelFromEntry(LevelEntry level)
    {
        if (level == null || level.levelTexture == null || !level.levelTexture.isReadable)
        {
            Debug.LogError("[LevelManager] Level entry is missing a readable texture.");
            return false;
        }

        if (slotPrefab == null || tilePrefab == null || boardContainer == null ||
            TrayManager.Instance == null || !TrayManager.Instance.IsConfigured)
        {
            Debug.LogError("[LevelManager] Assign the slot, tile, tray prefabs, and board/tray containers.");
            return false;
        }

        CurrentLevel = level;
        isTransitioning = false;
        ClearEntireGame();

        Texture2D tex = level.levelTexture;
        GridWidth = tex.width;
        GridHeight = tex.height;
        Grid = new BoardSlot[GridWidth, GridHeight];

        // 1. Calculate slot and socket dimensions
        if (level.manualSlotSize != Vector2.zero)
        {
            activeSlotSize = level.manualSlotSize;
            activeSocketSize = level.manualSocketSize;
        }
        else
        {
            float availableW = level.targetBoardAreaSize.x - ((GridWidth - 1) * level.spacing.x);
            float availableH = level.targetBoardAreaSize.y - ((GridHeight - 1) * level.spacing.y);
            float dimension = Mathf.Min(availableW / GridWidth, availableH / GridHeight);

            activeSlotSize = new Vector2(dimension, dimension);
            activeSocketSize = new Vector2(dimension * level.socketScaleFactor, dimension * level.socketScaleFactor);
        }

        // 2. Rebuild the bottom tray dynamically (matches grid width and socket size)
        TrayManager.Instance.RebuildTray(GridWidth, activeSocketSize, level.spacing);

        // 3. Compute grid centering coordinates
        float stepX = activeSlotSize.x + level.spacing.x;
        float stepY = activeSlotSize.y + level.spacing.y;
        float totalWidth = (GridWidth * activeSlotSize.x) + ((GridWidth - 1) * level.spacing.x);
        float totalHeight = (GridHeight * activeSlotSize.y) + ((GridHeight - 1) * level.spacing.y);

        float startX = -(totalWidth / 2f) + (activeSlotSize.x / 2f) + level.boardOffset.x;
        float startY = -(totalHeight / 2f) + (activeSlotSize.y / 2f) + level.boardOffset.y;

        List<Color> targetColors = new List<Color>();

        // 4. Instantiate and setup board slots
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                Color pixel = tex.GetPixel(x, y);
                if (pixel.a < 0.1f) continue;

                Vector2 pos = new Vector2(startX + (x * stepX), startY + (y * stepY));

                BoardSlot newSlot = Instantiate(slotPrefab, boardContainer);
                newSlot.name = $"Slot_{x}_{y}";

                RectTransform slotRt = newSlot.GetComponent<RectTransform>();
                slotRt.anchorMin = new Vector2(0.5f, 0.5f);
                slotRt.anchorMax = new Vector2(0.5f, 0.5f);
                slotRt.pivot = new Vector2(0.5f, 0.5f);
                slotRt.sizeDelta = activeSlotSize;
                slotRt.anchoredPosition = pos;

                newSlot.SetupSlot(new Vector2Int(x, y), pixel, level.spacing, activeSocketSize);

                Grid[x, y] = newSlot;
                AllActiveSlots.Add(newSlot);
                targetColors.Add(pixel);
            }
        }

        // 5. Shuffle the target colors without changing their counts
        List<Color> initialColors = CreateScrambledPool(targetColors);

        // 6. Instantiate movable tiles matching the active socket size
        for (int i = 0; i < AllActiveSlots.Count; i++)
        {
            Tile newTile = Instantiate(tilePrefab);
            newTile.name = $"Tile_{i}";

            RectTransform tileRt = newTile.GetComponent<RectTransform>();
            tileRt.anchorMin = new Vector2(0.5f, 0.5f);
            tileRt.anchorMax = new Vector2(0.5f, 0.5f);
            tileRt.pivot = new Vector2(0.5f, 0.5f);
            tileRt.sizeDelta = activeSocketSize;

            newTile.SetColor(initialColors[i]);
            AllActiveSlots[i].SetTile(newTile);
        }

        bool restoredSnapshot = TryRestoreLevelSnapshot(out bool restoredComplete);
        isTransitioning = restoredSnapshot && restoredComplete;
        if (!restoredSnapshot)
            SaveLevelSnapshot(false);

        Debug.Log($"<color=cyan>[LevelManager] Loaded {level.levelName} ({GridWidth}x{GridHeight}) successfully.</color>");
        LevelLoaded?.Invoke(level, currentLevelIndex, totalLevelCount);
        if (isTransitioning)
            LevelCompleted?.Invoke(currentLevelIndex == totalLevelCount - 1, false);
        return true;
    }

    private List<Color> CreateScrambledPool(List<Color> targets)
    {
        List<Color> pool = new List<Color>(targets);

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            (pool[i], pool[rnd]) = (pool[rnd], pool[i]);
        }

        bool hasMultipleColors = false;
        for (int i = 1; i < targets.Count; i++)
        {
            if (!BoardSlot.ColorsMatch(targets[0], targets[i]))
            {
                hasMultipleColors = true;
                break;
            }
        }

        bool stillInSolvedOrder = true;
        for (int i = 0; i < targets.Count; i++)
        {
            if (!BoardSlot.ColorsMatch(pool[i], targets[i]))
            {
                stillInSolvedOrder = false;
                break;
            }
        }

        if (hasMultipleColors && stillInSolvedOrder)
        {
            Color first = pool[0];
            pool.RemoveAt(0);
            pool.Add(first);
        }

        return pool;
    }
    public void RestartLevel()
    {
        if (selectedLevelData == null || totalLevelCount == 0) return;
        LevelSnapshotStore.Clear(currentLevelIndex);
        isTransitioning = false;
        StopAllCoroutines();
        LoadCurrentLevel();
    }

    public void OnSlotClicked(BoardSlot clickedSlot)
    {
        if (isTransitioning) return;

        if (CurrentlyPickedTiles.Count > 0)
        {
            if (clickedSlot.IsEmpty)
            {
                TryPlaceIntoConnectedTargetSlots(clickedSlot);
                return;
            }
        }

        if (clickedSlot.IsEmpty || clickedSlot.IsSatisfied) return;

        if (CurrentlyPickedTiles.Contains(clickedSlot.CurrentTile))
        {
            DeselectAll();
            return;
        }

        DeselectAll();

        List<BoardSlot> connectedSlots = PuzzleBoardSearch.FindConnectedUnsolvedColorGroup(Grid, clickedSlot);
        foreach (BoardSlot slot in connectedSlots)
        {
            if (slot.CurrentTile != null && !slot.IsSatisfied)
            {
                slot.CurrentTile.SetSelected(true, boardContainer, liftAmount: 26f);
                CurrentlyPickedTiles.Add(slot.CurrentTile);
            }
        }
    }

    public void SelectTilesFromTray(Color color)
    {
        if (isTransitioning) return;

        if (CurrentlyPickedTiles.Count > 0 &&
            CurrentlyPickedTiles[0].HomeTraySlot != null &&
            BoardSlot.ColorsMatch(CurrentlyPickedTiles[0].ColorValue, color))
        {
            DeselectAll();
            return;
        }

        DeselectAll();

        foreach (var slot in TrayManager.Instance.Slots)
        {
            if (!slot.IsEmpty && BoardSlot.ColorsMatch(slot.CurrentTile.ColorValue, color))
            {
                slot.CurrentTile.SetSelected(true, boardContainer, liftAmount: 26f);
                CurrentlyPickedTiles.Add(slot.CurrentTile);
            }
        }
    }

    private void TryPlaceIntoConnectedTargetSlots(BoardSlot startEmptySlot)
    {
        if (CurrentlyPickedTiles.Count == 0) return;

        Color pickedColor = CurrentlyPickedTiles[0].ColorValue;

        if (!BoardSlot.ColorsMatch(startEmptySlot.TargetFloorColor, pickedColor))
        {
            DeselectAll();
            return;
        }

        List<BoardSlot> connectedTargetSlots = PuzzleBoardSearch.FindConnectedEmptyTargetSlots(
            Grid, startEmptySlot, pickedColor);
        int countToMove = Mathf.Min(CurrentlyPickedTiles.Count, connectedTargetSlots.Count);

        for (int i = 0; i < countToMove; i++)
        {
            Tile tile = CurrentlyPickedTiles[i];
            BoardSlot dest = connectedTargetSlots[i];

            // Sever from source
            if (tile.HomeTraySlot != null)
            {
                tile.HomeTraySlot.ClearSlot();
            }
            else if (tile.HomeSlot != null)
            {
                tile.HomeSlot.CurrentTile = null;
            }

            // Register destination references
            dest.CurrentTile = tile;
            tile.AssignHomeSlot(dest);

            // Smooth tween animation into socket
            tile.AnimateMoveTo(dest.transform, activeSocketSize, boardContainer, () =>
            {
                CheckWinCondition();
            });
        }

        CurrentlyPickedTiles.RemoveRange(0, countToMove);
        SaveLevelSnapshot(AreAllSlotsSatisfied());
    }

    public void DeselectAll()
    {
        foreach (Tile t in CurrentlyPickedTiles)
        {
            if (t != null)
            {
                t.SetSelected(false, boardContainer);
            }
        }
        CurrentlyPickedTiles.Clear();
    }

    private void CheckWinCondition()
    {
        foreach (var slot in AllActiveSlots)
        {
            if (!slot.IsSatisfied) return;
        }

        if (!isTransitioning)
        {
            isTransitioning = true;
            highestUnlockedLevelIndex = Mathf.Max(
                highestUnlockedLevelIndex,
                Mathf.Min(currentLevelIndex + 1, totalLevelCount - 1));
            int resumeLevel = currentLevelIndex < totalLevelCount - 1
                ? currentLevelIndex + 1
                : currentLevelIndex;
            LevelProgressStore.SaveProgress(resumeLevel, highestUnlockedLevelIndex);
            SaveLevelSnapshot(true);
            bool isFinalLevel = currentLevelIndex == totalLevelCount - 1;
            LevelCompleted?.Invoke(isFinalLevel, true);
            StartCoroutine(AdvanceAfterCompletion());
        }
    }

    private IEnumerator AdvanceAfterCompletion()
    {
        yield return new WaitForSecondsRealtime(CompletionPopupDuration);

        if (currentLevelIndex >= totalLevelCount - 1)
        {
            SceneManager.LoadScene(SceneFlowBootstrap.SelectionSceneName);
            yield break;
        }

        LoadLevelAt(currentLevelIndex + 1);
    }

    private void LoadLevelAt(int levelIndex)
    {
        if (campaignLevels == null || levelIndex < 0 || levelIndex >= campaignLevels.Length)
        {
            Debug.LogError($"[LevelManager] Cannot load campaign level {levelIndex + 1}.");
            return;
        }

        LevelEntry level = campaignLevels[levelIndex];
        if (level == null || level.levelTexture == null || !level.levelTexture.isReadable)
        {
            Debug.LogError($"[LevelManager] Campaign level {levelIndex + 1} has invalid level data.");
            return;
        }

        currentLevelIndex = levelIndex;
        selectedLevelData = level;
        isTransitioning = false;
        LevelProgressStore.SaveCurrentLevelIndex(currentLevelIndex);

        if (!BuildLevelFromEntry(selectedLevelData))
            Debug.LogError($"[LevelManager] Could not build campaign level {currentLevelIndex + 1}.");
    }

    private bool TryRestoreLevelSnapshot(out bool completed)
    {
        completed = false;
        if (!LevelSnapshotStore.TryLoad(currentLevelIndex, out LevelSnapshotStore.Snapshot snapshot))
            return false;

        List<TraySlot> traySlots = TrayManager.Instance.Slots;
        bool valid = snapshot.schemaVersion == LevelSnapshotStore.CurrentSchemaVersion &&
                     snapshot.levelName == CurrentLevel.levelName &&
                     snapshot.textureName == CurrentLevel.levelTexture.name &&
                     snapshot.gridWidth == GridWidth && snapshot.gridHeight == GridHeight &&
                     snapshot.boardSlots != null && snapshot.boardSlots.Count == AllActiveSlots.Count &&
                     snapshot.traySlots != null && snapshot.traySlots.Count == traySlots.Count;

        if (valid)
        {
            for (int i = 0; i < AllActiveSlots.Count; i++)
            {
                LevelSnapshotStore.BoardSlotState savedSlot = snapshot.boardSlots[i];
                Vector2Int coords = AllActiveSlots[i].GridCoords;
                if (savedSlot == null || savedSlot.x != coords.x || savedSlot.y != coords.y)
                {
                    valid = false;
                    break;
                }
            }
        }

        if (valid)
        {
            for (int i = 0; i < traySlots.Count; i++)
            {
                if (snapshot.traySlots[i] == null || snapshot.traySlots[i].slotIndex != i)
                {
                    valid = false;
                    break;
                }
            }
        }

        if (!valid)
        {
            Debug.LogWarning($"[LevelManager] Saved state for '{CurrentLevel.levelName}' no longer matches this level. Starting fresh.");
            LevelSnapshotStore.Clear(currentLevelIndex);
            return false;
        }

        for (int i = 0; i < AllActiveSlots.Count; i++)
        {
            BoardSlot slot = AllActiveSlots[i];
            Tile generatedTile = slot.RemoveTile();
            if (generatedTile != null)
                Destroy(generatedTile.gameObject);

            LevelSnapshotStore.BoardSlotState savedSlot = snapshot.boardSlots[i];
            if (!savedSlot.hasTile)
                continue;

            Tile restoredTile = Instantiate(tilePrefab);
            restoredTile.name = $"RestoredTile_{savedSlot.x}_{savedSlot.y}";
            ConfigureTileRect(restoredTile.GetComponent<RectTransform>());
            restoredTile.SetColor(savedSlot.tileColor);
            slot.SetTile(restoredTile);
        }

        for (int i = 0; i < traySlots.Count; i++)
        {
            LevelSnapshotStore.TraySlotState savedSlot = snapshot.traySlots[i];
            if (!savedSlot.hasTile)
                continue;

            Tile restoredTile = Instantiate(tilePrefab);
            restoredTile.name = $"RestoredTrayTile_{i}";
            ConfigureTileRect(restoredTile.GetComponent<RectTransform>());
            restoredTile.SetColor(savedSlot.tileColor);
            traySlots[i].SetTile(restoredTile);
        }

        completed = snapshot.isComplete && AreAllSlotsSatisfied();
        Debug.Log($"[LevelManager] Restored saved progress for '{CurrentLevel.levelName}'.");
        return true;
    }

    private void ConfigureTileRect(RectTransform tileRect)
    {
        tileRect.anchorMin = new Vector2(0.5f, 0.5f);
        tileRect.anchorMax = new Vector2(0.5f, 0.5f);
        tileRect.pivot = new Vector2(0.5f, 0.5f);
        tileRect.sizeDelta = activeSocketSize;
    }

    private bool AreAllSlotsSatisfied()
    {
        if (AllActiveSlots.Count == 0)
            return false;

        foreach (BoardSlot slot in AllActiveSlots)
        {
            if (!slot.IsSatisfied)
                return false;
        }

        return true;
    }

    public void SaveCurrentLevelSnapshot()
    {
        SaveLevelSnapshot(isTransitioning);
    }

    private void SaveLevelSnapshot(bool completed)
    {
        if (Grid == null || CurrentLevel == null || TrayManager.Instance == null ||
            TrayManager.Instance.Slots == null || AllActiveSlots.Count == 0)
            return;

        LevelSnapshotStore.Snapshot snapshot = new LevelSnapshotStore.Snapshot
        {
            levelName = CurrentLevel.levelName,
            textureName = CurrentLevel.levelTexture != null ? CurrentLevel.levelTexture.name : string.Empty,
            gridWidth = GridWidth,
            gridHeight = GridHeight,
            isComplete = completed || isTransitioning
        };

        foreach (BoardSlot slot in AllActiveSlots)
        {
            snapshot.boardSlots.Add(new LevelSnapshotStore.BoardSlotState
            {
                x = slot.GridCoords.x,
                y = slot.GridCoords.y,
                hasTile = !slot.IsEmpty,
                tileColor = slot.IsEmpty ? Color.clear : slot.CurrentTile.ColorValue
            });
        }

        foreach (TraySlot slot in TrayManager.Instance.Slots)
        {
            snapshot.traySlots.Add(new LevelSnapshotStore.TraySlotState
            {
                slotIndex = slot.SlotIndex,
                hasTile = !slot.IsEmpty,
                tileColor = slot.IsEmpty ? Color.clear : slot.CurrentTile.ColorValue
            });
        }

        LevelSnapshotStore.Save(currentLevelIndex, snapshot);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveCurrentLevelSnapshot();
    }

    private void OnApplicationQuit()
    {
        SaveCurrentLevelSnapshot();
    }

    private void ClearEntireGame()
    {
        DeselectAll();
        AllActiveSlots.Clear();

        if (boardContainer != null)
        {
            for (int i = boardContainer.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(boardContainer.GetChild(i).gameObject);
                else
                    DestroyImmediate(boardContainer.GetChild(i).gameObject);
            }
        }

        if (TrayManager.Instance != null)
        {
            foreach (var slot in TrayManager.Instance.Slots)
            {
                if (!slot.IsEmpty)
                {
                    Tile t = slot.ClearSlot();
                    if (t != null) Destroy(t.gameObject);
                }
            }
        }
    }

}
