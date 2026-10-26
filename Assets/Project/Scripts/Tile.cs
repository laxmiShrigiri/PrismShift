using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Purpose: Represents a colored puzzle tile and handles its selection, clicks, and movement animation.
[RequireComponent(typeof(Image))]
public class Tile : MonoBehaviour, IPointerClickHandler
{
    public Color ColorValue { get; private set; }
    public BoardSlot HomeSlot { get; private set; }
    public TraySlot HomeTraySlot { get; private set; }

    public bool IsSelected { get; private set; }
    public bool IsMoving { get; private set; }

    private Image image;
    private RectTransform rectTransform;
    private Coroutine activeMoveRoutine;

    void Awake()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetColor(Color newColor)
    {
        if (image == null) image = GetComponent<Image>();
        ColorValue = newColor;
        image.color = newColor;
    }

    public void SetClickable(bool clickable)
    {
        if (image == null) image = GetComponent<Image>();
        image.raycastTarget = clickable;
    }

    public void AssignHomeSlot(BoardSlot slot)
    {
        HomeSlot = slot;
        HomeTraySlot = null;
    }

    public void AssignHomeTraySlot(TraySlot traySlot)
    {
        HomeTraySlot = traySlot;
        HomeSlot = null;
    }

    public void ForceDeselect()
    {
        IsSelected = false;
        if (activeMoveRoutine != null)
        {
            StopCoroutine(activeMoveRoutine);
            activeMoveRoutine = null;
        }
        transform.localScale = Vector3.one;
    }

    public void SetSelected(bool isSelected, RectTransform rootContainer, float liftAmount = 26f)
    {
        if (IsMoving) return;

        IsSelected = isSelected;
        if (activeMoveRoutine != null) StopCoroutine(activeMoveRoutine);

        if (isSelected)
        {
            transform.SetParent(rootContainer, true);
            transform.SetAsLastSibling();

            Vector2 basePos = GetHomeLocalPosition(rootContainer);
            Vector2 targetPos = basePos + new Vector2(0f, liftAmount);
            activeMoveRoutine = StartCoroutine(AnimateHover(targetPos, new Vector3(1.15f, 1.15f, 1f)));
        }
        else
        {
            Vector2 targetPos = GetHomeLocalPosition(rootContainer);
            activeMoveRoutine = StartCoroutine(ReturnToHomeRoutine(targetPos));
        }
    }

    /// <summary>
    /// Smoothly tweens this tile from current position to a destination transform (BoardSlot or TraySlot).
    /// Disables clicks during movement and re-enables interaction on arrival.
    /// </summary>
    public void AnimateMoveTo(Transform targetSlotTransform, Vector2 targetSize, RectTransform canvasRoot, Action onComplete = null)
    {
        if (activeMoveRoutine != null) StopCoroutine(activeMoveRoutine);
        activeMoveRoutine = StartCoroutine(MoveToTargetRoutine(targetSlotTransform, targetSize, canvasRoot, onComplete));
    }

    private IEnumerator MoveToTargetRoutine(Transform targetSlotTransform, Vector2 targetSize, RectTransform canvasRoot, Action onComplete)
    {
        IsMoving = true;
        IsSelected = false;
        SetClickable(false); // Disable clicks during movement

        // Keep attached to top container while flying
        transform.SetParent(canvasRoot, true);
        transform.SetAsLastSibling();

        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 startSize = rectTransform.sizeDelta;
        Vector3 startScale = rectTransform.localScale;

        // Convert target slot's world position to canvas-relative local coordinates
        Vector2 endPos;
        Camera canvasCamera = GetCanvasCamera(canvasRoot);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRoot,
            RectTransformUtility.WorldToScreenPoint(canvasCamera, targetSlotTransform.position),
            canvasCamera,
            out endPos
        );

        float duration = 0.22f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Smooth ease-out curve
            float ease = Mathf.Sin(t * Mathf.PI * 0.5f);

            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
            rectTransform.sizeDelta = Vector2.Lerp(startSize, targetSize, ease);
            rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one, ease);

            yield return null;
        }

        // Finalize landing
        transform.SetParent(targetSlotTransform, false);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = targetSize;
        rectTransform.localScale = Vector3.one;

        IsMoving = false;

        // Only tray tiles need to be clickable once settled (board satisfied tiles are locked)
        if (HomeTraySlot != null)
        {
            SetClickable(true);
        }
        else
        {
            SetClickable(false); // Locked in board socket
        }

        onComplete?.Invoke();
    }

    private Vector2 GetHomeLocalPosition(RectTransform container)
    {
        Transform homeTransform = HomeSlot != null ? HomeSlot.transform : (HomeTraySlot != null ? HomeTraySlot.transform : null);
        if (homeTransform != null)
        {
            Vector3 worldPos = homeTransform.position;
            Camera canvasCamera = GetCanvasCamera(container);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container,
                RectTransformUtility.WorldToScreenPoint(canvasCamera, worldPos),
                canvasCamera,
                out Vector2 localPos
            );
            return localPos;
        }
        return rectTransform.anchoredPosition;
    }

    private static Camera GetCanvasCamera(RectTransform container)
    {
        Canvas canvas = container != null ? container.GetComponentInParent<Canvas>() : null;
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
    }

    private IEnumerator AnimateHover(Vector2 targetPos, Vector3 targetScale)
    {
        float elapsed = 0f;
        float duration = 0.12f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector3 startScale = rectTransform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI * 0.5f);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPos;
        rectTransform.localScale = targetScale;
    }

    private IEnumerator ReturnToHomeRoutine(Vector2 targetPos)
    {
        float elapsed = 0f;
        float duration = 0.12f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector3 startScale = rectTransform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI * 0.5f);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one, t);
            yield return null;
        }

        if (HomeSlot != null)
        {
            transform.SetParent(HomeSlot.transform, false);
            rectTransform.anchoredPosition = Vector2.zero;
        }
        else if (HomeTraySlot != null)
        {
            transform.SetParent(HomeTraySlot.transform, false);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        rectTransform.localScale = Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsMoving) return; // Ignore input while animating

        if (IsSelected)
        {
            LevelManager.Instance.DeselectAll();
            return;
        }

        if (HomeTraySlot != null)
        {
            TrayManager.Instance.OnTraySlotClicked(HomeTraySlot);
            return;
        }

        if (HomeSlot != null)
        {
            LevelManager.Instance.OnSlotClicked(HomeSlot);
        }
    }
}
