// using UnityEngine;
// using UnityEngine.UI;

// [RequireComponent(typeof(Image))]
// public class MatchBackgroundColorUI : MonoBehaviour
// {
//     [SerializeField] private Image backgroundImage;
//     private Image boxImage;
//     private RectTransform boxRect;
//     private RectTransform bgRect;

//     void Awake()
//     {
//         boxImage = GetComponent<Image>();
//         boxRect = GetComponent<RectTransform>();
//         bgRect = backgroundImage.GetComponent<RectTransform>();
//     }

//     void Start()
//     {
//         MatchColor();
//     }

//     [ContextMenu("Match Color")]
//     public void MatchColor()
//     {
//         if (backgroundImage == null || backgroundImage.sprite == null) return;

//         Sprite bgSprite = backgroundImage.sprite;
//         Texture2D texture = bgSprite.texture;

//         // Convert box center to the background's local rect space
//         Vector2 localPoint;
//         RectTransformUtility.ScreenPointToLocalPointInRectangle(
//             bgRect,
//             RectTransformUtility.WorldToScreenPoint(null, boxRect.position),
//             null,
//             out localPoint
//         );

//         // Normalize point inside the background Rect
//         Rect r = bgRect.rect;
//         float u = Mathf.Clamp01((localPoint.x - r.xMin) / r.width);
//         float v = Mathf.Clamp01((localPoint.y - r.yMin) / r.height);

//         // Map to texture rect
//         Rect spriteRect = bgSprite.rect;
//         int pixelX = Mathf.FloorToInt(spriteRect.x + (u * spriteRect.width));
//         int pixelY = Mathf.FloorToInt(spriteRect.y + (v * spriteRect.height));

//         boxImage.color = texture.GetPixel(pixelX, pixelY);
//     }
// }

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Purpose: In the older prototype, samples the background image under a UI element and applies that color.
[RequireComponent(typeof(Image))]
public class MatchBackgroundColorUI : MonoBehaviour
{
    [Tooltip("Drag the background Image here (or leave empty to auto-detect a parent/sibling named 'Background')")]
    [SerializeField] private Image backgroundImage;

    private Image boxImage;
    private RectTransform boxRect;

    void Awake()
    {
        boxImage = GetComponent<Image>();
        boxRect = GetComponent<RectTransform>();

        // Auto-detect background if not manually assigned
        if (backgroundImage == null)
        {
            Transform bgTransform = transform.parent?.Find("Background");
            if (bgTransform != null)
                backgroundImage = bgTransform.GetComponent<Image>();
        }
    }

    IEnumerator Start()
    {
        // Critical: Wait until Unity's layout group finishes computing X and Y coordinates
        yield return new WaitForEndOfFrame();
        SampleAndApplyColor();
    }

    [ContextMenu("Sample Color Now")]
    public void SampleAndApplyColor()
    {
        if (backgroundImage == null || backgroundImage.sprite == null)
        {
            Debug.LogWarning($"[DynamicBackgroundSampler] Missing Background Image reference on {gameObject.name}");
            return;
        }

        RectTransform bgRect = backgroundImage.rectTransform;
        Sprite bgSprite = backgroundImage.sprite;
        Texture2D texture = bgSprite.texture;

        // Ensure texture is readable
        if (!texture.isReadable)
        {
            Debug.LogError($"[DynamicBackgroundSampler] Texture '{texture.name}' must have 'Read/Write Enabled' checked in its Import Settings!");
            return;
        }

        // Get active camera (null if Canvas is Screen Space - Overlay)
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        // 1. Get the center point of this box in screen coordinates
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, boxRect.position);

        // 2. Convert screen point to local coordinates inside the background RectTransform
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bgRect, screenPoint, cam, out Vector2 localPoint))
            return;

        // 3. Normalize local point to UV coordinates (0.0 to 1.0)
        Rect r = bgRect.rect;
        float u = Mathf.Clamp01((localPoint.x - r.xMin) / r.width);
        float v = Mathf.Clamp01((localPoint.y - r.yMin) / r.height);

        // 4. Map normalized UV to the actual pixel rect of the sprite within its texture
        Rect spriteRect = bgSprite.rect;
        int pixelX = Mathf.Clamp(Mathf.FloorToInt(spriteRect.x + (u * spriteRect.width)), 0, texture.width - 1);
        int pixelY = Mathf.Clamp(Mathf.FloorToInt(spriteRect.y + (v * spriteRect.height)), 0, texture.height - 1);

        // 5. Read pixel and apply color
        Color detectedColor = texture.GetPixel(pixelX, pixelY);
        boxImage.color = detectedColor;
    }
}
