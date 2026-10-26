using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Purpose: In the older prototype, fills a tile when the player's selected color matches its target.
[RequireComponent(typeof(Image))]
public class GemTile : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler
{
    [Header("Required Target Color ID (1-4)")]
    public int targetColorId = 1;

    [Header("References")]
    [SerializeField] private Text numberLabel; // Text child showing the number (e.g. "1")

    private Image tileImage;
    private bool isFilled = false;

    void Awake()
    {
        tileImage = GetComponent<Image>();
    }

    void Start()
    {
        if (DiamondArtManager.Instance != null)
        {
            DiamondArtManager.Instance.RegisterTile();
        }

        // Show the target number initially
        if (numberLabel != null)
        {
            numberLabel.text = targetColorId.ToString();
        }

        // Default empty tile appearance (light grey)
        tileImage.color = new Color(0.92f, 0.92f, 0.92f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TryPaint();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Allows drag-painting while holding left click / touch
        if (Input.GetMouseButton(0))
        {
            TryPaint();
        }
    }

    private void TryPaint()
    {
        if (isFilled) return;

        if (DiamondArtManager.Instance.selectedColorId == targetColorId)
        {
            isFilled = true;
            tileImage.color = DiamondArtManager.Instance.GetColorById(targetColorId);

            // Hide the number once filled
            if (numberLabel != null)
            {
                numberLabel.gameObject.SetActive(false);
            }

            DiamondArtManager.Instance.OnTileFilled();
        }
    }
}
