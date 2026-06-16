using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeController : MonoBehaviour
{
    [Header("Layout")]
    public RectTransform content;
    public RectTransform academicView;
    public RectTransform playableView;

    [Header("Animation")]
    public float swipeSpeed = 10f;
    public float swipeThreshold = 100f;

    private Vector2 academicPosition;
    private Vector2 playablePosition;
    private Vector2 targetPosition;
    private bool pagesCached;

    private float startTouchX;
    private bool isDragging;
    private int activePointerId = -1;

    void Start()
    {
        if (content == null || academicView == null || playableView == null)
        {
            Debug.LogError("SwipeController: assign Content, AcademicView and PlayableView.");
            enabled = false;
            return;
        }

        CachePagePositions();
        SnapToClosestPage();
    }

    void Update()
    {
        HandleSwipe();

        content.anchoredPosition = Vector2.Lerp(
            content.anchoredPosition,
            targetPosition,
            Time.deltaTime * swipeSpeed
        );
    }

    private void CachePagePositions()
    {
        if (content == null || academicView == null || playableView == null)
            return;

        // «Домашнюю» (академическую) позицию Content кэшируем ОДИН раз — это её
        // авторская позиция в сцене. Иначе при повторных вызовах SnapToPlayable
        // за «академическую» принимается уже прокрученное положение, и страница
        // каждый раз уезжает ещё на один экран в сторону.
        if (!pagesCached)
        {
            academicPosition = content.anchoredPosition;
            pagesCached = true;
        }

        float pageOffset = playableView.anchoredPosition.x - academicView.anchoredPosition.x;
        playablePosition = academicPosition - new Vector2(pageOffset, 0f);
    }

    private void SnapToClosestPage()
    {
        float distanceToAcademic = Vector2.Distance(content.anchoredPosition, academicPosition);
        float distanceToPlayable = Vector2.Distance(content.anchoredPosition, playablePosition);

        targetPosition = distanceToAcademic <= distanceToPlayable
            ? academicPosition
            : playablePosition;

        content.anchoredPosition = targetPosition;
    }

    private void HandleSwipe()
    {
        if (Input.touchCount > 0)
        {
            HandleTouchSwipe();
            return;
        }

        HandleMouseSwipe();
    }

    private void HandleTouchSwipe()
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            if (IsPointerOverUi(touch.fingerId))
            {
                isDragging = false;
                activePointerId = -1;
                return;
            }

            startTouchX = touch.position.x;
            isDragging = true;
            activePointerId = touch.fingerId;
            return;
        }

        if (!isDragging || touch.fingerId != activePointerId)
        {
            return;
        }

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            ApplySwipeDelta(touch.position.x - startTouchX);
            isDragging = false;
            activePointerId = -1;
        }
    }

    private void HandleMouseSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUi())
            {
                isDragging = false;
                return;
            }

            startTouchX = Input.mousePosition.x;
            isDragging = true;
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            ApplySwipeDelta(Input.mousePosition.x - startTouchX);
            isDragging = false;
        }
    }

    private static bool IsPointerOverUi(int pointerId = -1)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return pointerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(pointerId)
            : EventSystem.current.IsPointerOverGameObject();
    }

    private void ApplySwipeDelta(float delta)
    {
        if (delta < -swipeThreshold)
        {
            ShowPlayable();
        }
        else if (delta > swipeThreshold)
        {
            ShowAcademic();
        }
    }

    public void ShowAcademic()
    {
        targetPosition = academicPosition;
    }

    public void ShowPlayable()
    {
        targetPosition = playablePosition;
    }

    public void SnapToPlayable()
    {
        CachePagePositions();
        targetPosition = playablePosition;
        if (content != null)
            content.anchoredPosition = playablePosition;
    }
}
