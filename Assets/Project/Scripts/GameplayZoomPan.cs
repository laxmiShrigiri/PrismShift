using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

// Purpose: Lets players pinch-zoom and drag the puzzle board and tile tray on a touch screen.
// The fixed UI stays outside the transformed gameplay content.
[RequireComponent(typeof(Canvas))]
public sealed class GameplayZoomPan : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Gameplay Containers")]
    [SerializeField] private RectTransform boardContainer;
    [SerializeField] private RectTransform trayContainer;

    [Header("Zoom Settings")]
    [SerializeField, Min(1f)] private float minimumZoom = 1f;
    [SerializeField, Min(1f)] private float maximumZoom = 3f;
    [SerializeField, Min(1.01f)] private float mouseZoomStep = 1.15f;

    private Canvas gameplayCanvas;
    private RectTransform gameplayViewport;
    private RectTransform gameplayContent;
    private Camera eventCamera;
    private float currentZoom = 1f;
    private float previousPinchDistance;
    private bool pinchActive;
    private bool panDragActive;
    private Bounds cachedGameplayBounds;
    private int cachedLevelIndex = int.MinValue;
    private bool hasCachedGameplayBounds;

    private EventSystem cachedEventSystem;
    private PointerEventData raycastPointerData;
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private void Awake()
    {
        gameplayCanvas = GetComponent<Canvas>();
        if (boardContainer == null || trayContainer == null)
        {
            Debug.LogError("[GameplayZoomPan] Assign the board and tray containers in the Inspector.");
            enabled = false;
            return;
        }

        eventCamera = gameplayCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : gameplayCanvas.worldCamera;

        CreateGameplayViewport();
    }

    private void OnDisable()
    {
        pinchActive = false;
        panDragActive = false;
    }

    private void Update()
    {
        if (ProcessTouchPinch())
            return;

        ProcessMouseWheelZoom();
    }

    private void CreateGameplayViewport()
    {
        // This fixed, transparent viewport receives gameplay drags. Its child content is what moves.
        GameObject viewportObject = new GameObject(
            "GameplayViewport",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));
        viewportObject.layer = gameObject.layer;

        gameplayViewport = viewportObject.GetComponent<RectTransform>();
        gameplayViewport.SetParent(transform, false);
        SetFullScreenRect(gameplayViewport);
        gameplayViewport.SetAsFirstSibling();

        RawImage inputSurface = viewportObject.GetComponent<RawImage>();
        inputSurface.texture = Texture2D.whiteTexture;
        inputSurface.color = new Color(1f, 1f, 1f, 0f);
        inputSurface.raycastTarget = true;

        GameObject contentObject = new GameObject("GameplayContent", typeof(RectTransform));
        contentObject.layer = gameObject.layer;
        gameplayContent = contentObject.GetComponent<RectTransform>();
        gameplayContent.SetParent(gameplayViewport, false);
        SetFullScreenRect(gameplayContent);

        // Preserve the scene's draw order: the tray is behind the board, so selected tiles
        // reparented into BoardContainer render above tray tiles while they are lifted.
        trayContainer.SetParent(gameplayContent, true);
        boardContainer.SetParent(gameplayContent, true);
        boardContainer.SetAsLastSibling();
    }

    private static void SetFullScreenRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private bool ProcessTouchPinch()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
        {
            pinchActive = false;
            previousPinchDistance = 0f;
            return false;
        }

        TouchControl firstTouch = null;
        TouchControl secondTouch = null;
        foreach (TouchControl touch in touchscreen.touches)
        {
            if (!touch.press.isPressed)
                continue;

            if (firstTouch == null)
                firstTouch = touch;
            else
            {
                secondTouch = touch;
                break;
            }
        }

        if (secondTouch == null)
        {
            pinchActive = false;
            previousPinchDistance = 0f;
            return false;
        }

        // A pinch takes over from panning and must not resume a pan when one finger lifts.
        panDragActive = false;

        Vector2 firstPosition = firstTouch.position.ReadValue();
        Vector2 secondPosition = secondTouch.position.ReadValue();
        if (!IsGameplayPoint(firstPosition) || !IsGameplayPoint(secondPosition))
        {
            pinchActive = false;
            previousPinchDistance = 0f;
            return true;
        }

        float distance = Vector2.Distance(firstPosition, secondPosition);
        if (distance <= 0f)
            return true;

        if (!pinchActive || previousPinchDistance <= 0f)
        {
            pinchActive = true;
            previousPinchDistance = distance;
            return true;
        }

        Vector2 midpoint = (firstPosition + secondPosition) * 0.5f;
        ZoomAtScreenPoint(distance / previousPinchDistance, midpoint);
        previousPinchDistance = distance;
        return true;
    }

    private void ProcessMouseWheelZoom()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f)
            return;

        Vector2 screenPosition = mouse.position.ReadValue();
        if (IsGameplayPoint(screenPosition))
            ZoomAtScreenPoint(Mathf.Pow(mouseZoomStep, scroll / 120f), screenPosition);
    }

    private void ZoomAtScreenPoint(float zoomFactor, Vector2 screenPosition)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gameplayViewport, screenPosition, eventCamera, out Vector2 viewportPoint))
            return;

        float targetZoom = Mathf.Clamp(currentZoom * zoomFactor, minimumZoom, maximumZoom);
        if (Mathf.Approximately(targetZoom, currentZoom))
            return;

        // Adjust the content offset so the point between the fingers stays under the same screen point.
        Vector2 contentPoint = (viewportPoint - gameplayContent.anchoredPosition) / currentZoom;
        currentZoom = targetZoom;
        gameplayContent.localScale = Vector3.one * currentZoom;
        gameplayContent.anchoredPosition = viewportPoint - contentPoint * currentZoom;
        ClampContentPosition();
    }

    private bool IsGameplayPoint(Vector2 screenPosition)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(gameplayViewport, screenPosition, eventCamera))
            return false;

        EventSystem currentEventSystem = EventSystem.current;
        if (currentEventSystem == null)
            return true;

        if (cachedEventSystem != currentEventSystem)
        {
            cachedEventSystem = currentEventSystem;
            raycastPointerData = new PointerEventData(cachedEventSystem);
        }

        raycastPointerData.position = screenPosition;
        raycastResults.Clear();
        cachedEventSystem.RaycastAll(raycastPointerData, raycastResults);

        // The separate menu canvas is above gameplay, so button areas do not start a zoom gesture.
        if (raycastResults.Count == 0 || raycastResults[0].gameObject == null)
            return true;

        Transform hitTransform = raycastResults[0].gameObject.transform;
        return hitTransform == gameplayViewport || hitTransform.IsChildOf(gameplayViewport);
    }

    private void ClampContentPosition()
    {
        if (boardContainer == null || trayContainer == null || gameplayContent == null)
            return;

        int currentLevelIndex = LevelManager.Instance != null
            ? LevelManager.Instance.CurrentLevelIndex
            : -1;
        if (!hasCachedGameplayBounds || cachedLevelIndex != currentLevelIndex)
        {
            cachedGameplayBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                gameplayContent, boardContainer);
            cachedGameplayBounds.Encapsulate(RectTransformUtility.CalculateRelativeRectTransformBounds(
                gameplayContent, trayContainer));
            cachedLevelIndex = currentLevelIndex;
            hasCachedGameplayBounds = true;
        }

        Bounds contentBounds = cachedGameplayBounds;
        float halfWidth = gameplayViewport.rect.width * 0.5f;
        float halfHeight = gameplayViewport.rect.height * 0.5f;
        Vector2 position = gameplayContent.anchoredPosition;

        // If all content fits on an axis, keep its original layout centered on that axis.
        if (contentBounds.size.x * currentZoom <= gameplayViewport.rect.width)
            position.x = 0f;
        else
            position.x = Mathf.Clamp(position.x,
                halfWidth - contentBounds.max.x * currentZoom,
                -halfWidth - contentBounds.min.x * currentZoom);

        if (contentBounds.size.y * currentZoom <= gameplayViewport.rect.height)
            position.y = 0f;
        else
            position.y = Mathf.Clamp(position.y,
                halfHeight - contentBounds.max.y * currentZoom,
                -halfHeight - contentBounds.min.y * currentZoom);

        gameplayContent.anchoredPosition = position;
    }

    private bool HasMultipleTouchesPressed()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
            return false;

        int pressedTouchCount = 0;
        foreach (TouchControl touch in touchscreen.touches)
        {
            if (touch.press.isPressed && ++pressedTouchCount >= 2)
                return true;
        }

        return false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        panDragActive = currentZoom > minimumZoom + 0.001f &&
                        !pinchActive && !HasMultipleTouchesPressed();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!panDragActive || pinchActive || HasMultipleTouchesPressed())
            return;

        Vector2 previousScreenPoint = eventData.position - eventData.delta;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gameplayViewport, previousScreenPoint, eventCamera, out Vector2 previousPoint) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gameplayViewport, eventData.position, eventCamera, out Vector2 currentPoint))
            return;

        gameplayContent.anchoredPosition += currentPoint - previousPoint;
        ClampContentPosition();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        panDragActive = false;
    }
}
