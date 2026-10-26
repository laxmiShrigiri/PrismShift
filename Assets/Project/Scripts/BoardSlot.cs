using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Purpose: Represents one board cell, including its target color and the tile currently in it.
public class BoardSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Slot Data")]
    public Vector2Int GridCoords;
    public Color TargetFloorColor;
    public Tile CurrentTile;

    [Header("Layer References")]
    [SerializeField] private Image targetColorImage;
    [SerializeField] private Image socketImage;

    public bool IsEmpty => CurrentTile == null;
    public bool IsSatisfied => CurrentTile != null && ColorsMatch(CurrentTile.ColorValue, TargetFloorColor);

    public void SetupSlot(Vector2Int coords, Color floorColor, Vector2 spacing, Vector2 socketDimensions)
    {
        GridCoords = coords;
        TargetFloorColor = floorColor;

        // Layer 1: Base color spans across slots to remove gaps
        if (targetColorImage != null)
        {
            RectTransform baseRt = targetColorImage.rectTransform;
            RectTransform rootRt = GetComponent<RectTransform>();

            baseRt.anchorMin = new Vector2(0.5f, 0.5f);
            baseRt.anchorMax = new Vector2(0.5f, 0.5f);
            baseRt.pivot = new Vector2(0.5f, 0.5f);
            baseRt.sizeDelta = rootRt.sizeDelta + spacing;
            baseRt.anchoredPosition = Vector2.zero;

            Color baseColor = floorColor;
            baseColor.a = 1f;
            targetColorImage.color = baseColor;
        }

        // Layer 2: Explicitly size the socket cavity
        if (socketImage != null)
        {
            RectTransform socketRt = socketImage.rectTransform;
            socketRt.anchorMin = new Vector2(0.5f, 0.5f);
            socketRt.anchorMax = new Vector2(0.5f, 0.5f);
            socketRt.pivot = new Vector2(0.5f, 0.5f);
            socketRt.sizeDelta = socketDimensions;
            socketRt.anchoredPosition = Vector2.zero;

            socketImage.color = new Color(1f, 1f, 1f, 0.9f);
        }
    }

    public void SetTile(Tile tile)
    {
        CurrentTile = tile;
        if (tile != null)
        {
            tile.AssignHomeSlot(this);
            tile.transform.SetParent(transform, false);
            tile.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            tile.transform.localScale = Vector3.one;
            tile.transform.SetAsLastSibling();
        }
    }

    public Tile RemoveTile()
    {
        Tile temp = CurrentTile;
        CurrentTile = null;
        return temp;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnSlotClicked(this);
        }
    }

    public static bool ColorsMatch(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.05f &&
               Mathf.Abs(a.g - b.g) < 0.05f &&
               Mathf.Abs(a.b - b.b) < 0.05f;
    }
}
