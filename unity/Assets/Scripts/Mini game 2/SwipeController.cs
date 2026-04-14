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

    private float startTouchX;
    private bool isDragging;

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
        academicPosition = content.anchoredPosition;

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
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                isDragging = false;
                return;
            }

            startTouchX = Input.mousePosition.x;
            isDragging = true;
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            float delta = Input.mousePosition.x - startTouchX;

            if (delta < -swipeThreshold)
                ShowPlayable();
            else if (delta > swipeThreshold)
                ShowAcademic();

            isDragging = false;
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
}
