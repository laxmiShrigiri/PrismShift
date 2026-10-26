using System.Collections.Generic;
using UnityEngine;

// Purpose: Creates the tile storage tray for each level and manages moving tiles into and out of it.
public class TrayManager : MonoBehaviour
{
    public static TrayManager Instance { get; private set; }

    [Header("Tray Prefab & Parent")]
    [SerializeField] private GameObject traySlotPrefab;
    [SerializeField] private RectTransform trayContainer;

    [Header("Positioning")]
    [SerializeField] private float trayPositionY = -420f;
    [SerializeField] private Vector2 traySpacing = new Vector2(8f, 0f);

    public List<TraySlot> Slots { get; private set; } = new List<TraySlot>();
    public int CurrentCapacity { get; private set; }
    public Vector2 CurrentSlotSize { get; private set; }
    public bool IsConfigured => traySlotPrefab != null && trayContainer != null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[TrayManager] Only one TrayManager can be active in a scene.");
            enabled = false;
            return;
        }

        Instance = this;
        if (trayContainer == null)
            trayContainer = GetComponent<RectTransform>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Dynamically rebuilds the tray to match the level's grid width and socket dimensions.
    /// </summary>
    public void RebuildTray(int capacity, Vector2 socketSize, Vector2 spacing)
    {
        CurrentCapacity = capacity;
        CurrentSlotSize = socketSize;
        if (spacing != Vector2.zero) traySpacing = spacing;

        if (!IsConfigured || capacity <= 0)
        {
            Debug.LogError("[TrayManager] Assign a tray slot prefab and container, and provide positive capacity.");
            return;
        }

        // Clear existing slots and lingering tiles
        for (int i = trayContainer.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(trayContainer.GetChild(i).gameObject);
            else
                DestroyImmediate(trayContainer.GetChild(i).gameObject);
        }
        Slots.Clear();

        float stepX = CurrentSlotSize.x + traySpacing.x;
        float totalWidth = (capacity * CurrentSlotSize.x) + ((capacity - 1) * traySpacing.x);
        float startX = -(totalWidth / 2f) + (CurrentSlotSize.x / 2f);

        for (int i = 0; i < capacity; i++)
        {
            GameObject slotObj = Instantiate(traySlotPrefab, trayContainer);
            slotObj.name = $"TraySlot_{i}";

            RectTransform rt = slotObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = CurrentSlotSize;
            rt.anchoredPosition = new Vector2(startX + (i * stepX), trayPositionY);

            TraySlot traySlot = slotObj.GetComponent<TraySlot>();
            if (traySlot == null) traySlot = slotObj.AddComponent<TraySlot>();
            traySlot.SlotIndex = i;

            Slots.Add(traySlot);
        }

        Debug.Log($"<color=cyan>[TrayManager] Rebuilt tray with {capacity} slots. Size: {CurrentSlotSize}</color>");
    }

    public List<TraySlot> GetEmptySlots()
    {
        List<TraySlot> emptyList = new List<TraySlot>();
        foreach (var s in Slots)
        {
            if (s.IsEmpty) emptyList.Add(s);
        }
        return emptyList;
    }

    public void OnTraySlotClicked(TraySlot clickedSlot)
    {
        if (LevelManager.Instance == null || clickedSlot == null)
            return;

        // CASE 1: Holding tiles from the board -> Deposit into empty tray slots
        if (LevelManager.Instance.CurrentlyPickedTiles.Count > 0 &&
            LevelManager.Instance.CurrentlyPickedTiles[0].HomeSlot != null)
        {
            DepositSelectedTilesToTray();
            return;
        }

        // CASE 2: Clicked an occupied slot in the tray -> Select matching color tiles
        if (!clickedSlot.IsEmpty)
        {
            LevelManager.Instance.SelectTilesFromTray(clickedSlot.CurrentTile.ColorValue);
            return;
        }

        // CASE 3: Clicked an empty slot while holding tray tiles -> Deselect
        if (LevelManager.Instance.CurrentlyPickedTiles.Count > 0)
        {
            LevelManager.Instance.DeselectAll();
        }
    }

    public void DepositSelectedTilesToTray()
    {
        List<TraySlot> emptySlots = GetEmptySlots();

        if (emptySlots.Count == 0)
        {
            Debug.Log("[TrayManager] Tray is full!");
            LevelManager.Instance.DeselectAll();
            return;
        }

        int countToMove = Mathf.Min(LevelManager.Instance.CurrentlyPickedTiles.Count, emptySlots.Count);

        for (int i = 0; i < countToMove; i++)
        {
            Tile tile = LevelManager.Instance.CurrentlyPickedTiles[i];
            TraySlot targetSlot = emptySlots[i];

            if (tile.HomeSlot != null)
            {
                tile.HomeSlot.CurrentTile = null;
            }

            // Assign slot state immediately so logic stays consistent
            targetSlot.CurrentTile = tile;
            tile.AssignHomeTraySlot(targetSlot);

            // Animate movement to tray slot
            tile.AnimateMoveTo(targetSlot.transform, CurrentSlotSize, LevelManager.Instance.BoardContainer);
        }

        LevelManager.Instance.CurrentlyPickedTiles.RemoveRange(0, countToMove);

        // Ground leftover tiles
        foreach (Tile remaining in LevelManager.Instance.CurrentlyPickedTiles)
        {
            if (remaining != null && remaining.HomeSlot != null)
            {
                remaining.SetSelected(false, LevelManager.Instance.BoardContainer);
            }
        }

        LevelManager.Instance.CurrentlyPickedTiles.Clear();
        LevelManager.Instance.SaveCurrentLevelSnapshot();
    }
}
