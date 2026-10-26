using UnityEngine;
using UnityEngine.EventSystems;

// Purpose: Represents one temporary tray cell and forwards clicks to the tray manager.
public class TraySlot : MonoBehaviour, IPointerClickHandler
{
    public int SlotIndex;
    public Tile CurrentTile;

    public bool IsEmpty => CurrentTile == null;

    public void SetTile(Tile tile)
    {
        CurrentTile = tile;
        if (tile != null)
        {
            tile.AssignHomeTraySlot(this);
            tile.transform.SetParent(transform, false);

            // Match the dynamic socket dimensions
            RectTransform slotRt = GetComponent<RectTransform>();
            RectTransform tileRt = tile.GetComponent<RectTransform>();
            tileRt.sizeDelta = slotRt.sizeDelta;
            tileRt.anchoredPosition = Vector2.zero;

            tile.transform.localScale = Vector3.one;
            tile.transform.SetAsLastSibling();
        }
    }
    public Tile ClearSlot()
    {
        Tile temp = CurrentTile;
        CurrentTile = null;
        return temp;
    }

   public void OnPointerClick(PointerEventData eventData)
    {
        if (CurrentTile != null && CurrentTile.IsMoving) return;

        if (TrayManager.Instance != null)
        {
            TrayManager.Instance.OnTraySlotClicked(this);
        }
    }
}
