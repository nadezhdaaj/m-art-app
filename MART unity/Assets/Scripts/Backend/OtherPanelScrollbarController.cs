using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif



/// <summary>

/// Wires the scene "scrollbar" on OtherPanel to vertically scroll panel content.

/// Does not change carousel, buttons, or other panel mechanics.

/// </summary>

[DisallowMultipleComponent]

[ExecuteAlways]

public class OtherPanelScrollbarController : MonoBehaviour

{

    public const string ScrollbarObjectName = "scrollbar";

    public const string ScrollContentName = "OtherPanelScrollContent";

    public const string SeeAllButtonName = "See All";

    public const string UserNotesSectionName = "user notes";

    public const string NotesScrollBoundaryMarkerName = "Personal impressions, description..";



    private const float TrackPaddingBottom = 12f;

    private const float ScrollPaddingBelowBoundary = 16f;

    private float GetEffectiveScrollPadding()
    {
        return ScrollPaddingBelowBoundary;
    }

    private float GetVisiblePanelBottomY()
    {
        float panelBottom = -panelRect.rect.height * 0.5f;
        RectTransform bottomBar = FindBottomBarRect();
        if (bottomBar == null)
        {
            return panelBottom;
        }

        Bounds barInPanel = RectTransformUtility.CalculateRelativeRectTransformBounds(panelRect, bottomBar);
        return Mathf.Max(panelBottom, barInPanel.max.y);
    }

    private static RectTransform FindBottomBarRect()
    {
        GameObject bottomBar = GameObject.Find("BottomBar");
        return bottomBar != null ? bottomBar.transform as RectTransform : null;
    }



    private static readonly string[] IgnoredContentChildNames =

    {

        "Design elements",

        "carousel ",

        "carousel",

        "Gallery",

        ScrollbarObjectName,

    };



    private RectTransform panelRect;

    private RectTransform contentRect;

    private RectTransform scrollbarRect;

    private OtherPanelScrollbarDrag scrollbarDrag;

    private float cachedMaxScroll;



    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]

    private static void AutoConfigureOnMainStage()

    {

        if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("main stage"))

        {

            return;

        }



        Ensure();

    }



    public static void Ensure()

    {

        OtherPanelScrollbarController controller = FindController();

        if (controller == null)

        {

            return;

        }



        controller.Configure();

    }



    public static void RefreshLayout()

    {

        OtherPanelScrollbarController controller = FindController();

        if (controller == null)

        {

            Ensure();

            controller = FindController();

        }



        controller?.RefreshContentMetrics();

    }



    private static OtherPanelScrollbarController FindController()

    {

        GameObject otherPanel = FindOtherPanel();

        return otherPanel != null ? otherPanel.GetComponent<OtherPanelScrollbarController>() : null;

    }



    private static GameObject FindOtherPanel()

    {

        GameObject otherPanel = GameObject.Find("OtherPanel");

        if (otherPanel != null)

        {

            return otherPanel;

        }



        OtherPanelScrollbarController[] controllers = Object.FindObjectsOfType<OtherPanelScrollbarController>(true);

        return controllers.Length > 0 ? controllers[0].gameObject : null;

    }



    private void OnEnable()
    {
        Configure();

        if (Application.isPlaying)
        {
            ResetScrollToInitial();
            StartCoroutine(RefreshAfterLayoutSettled());
            return;
        }

#if UNITY_EDITOR
        EditorApplication.delayCall += DeferredEditorRefresh;
#endif
    }

#if UNITY_EDITOR
    private void DeferredEditorRefresh()
    {
        if (this == null || contentRect == null)
        {
            return;
        }

        RefreshContentMetrics();
    }
#endif



    private IEnumerator RefreshAfterLayoutSettled()

    {

        yield return null;

        RefreshContentMetrics();

        yield return null;

        RefreshContentMetrics();

    }



#if UNITY_EDITOR

    private void OnValidate()

    {

        if (!Application.isPlaying)

        {

            Configure();

        }

    }

#endif



    public void Configure()

    {

        panelRect = transform as RectTransform;

        if (panelRect == null)

        {

            return;

        }



        if (!OtherPanelScrollbarHierarchy.IsBaked(transform))

        {

            OtherPanelScrollbarHierarchy.Bake(transform, registerUndo: false);

        }



        EnsureViewportMask();

        contentRect = transform.Find(ScrollContentName) as RectTransform;

        scrollbarRect = FindScrollbar();

        if (contentRect == null || scrollbarRect == null)

        {

            return;

        }



        EnsureScrollbarOnPanel();

        PrepareScrollbarForDrag();

        WireScrollbarDrag();

        EnsureNotesScrollAdapter();

        EnsureScrollbarVisible();

        RefreshContentMetrics();

    }



    private void EnsureNotesScrollAdapter()
    {
        RectTransform userNotes = FindUserNotes();
        if (userNotes == null)
        {
            return;
        }

        if (userNotes.GetComponent<OtherPanelNotesScrollAdapter>() == null)
        {
            userNotes.gameObject.AddComponent<OtherPanelNotesScrollAdapter>();
        }
    }



    public void ResetScrollToInitial()

    {

        if (contentRect == null)

        {

            return;

        }



        contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, 0f);

        scrollbarDrag?.RefreshTrackLimits();

        scrollbarDrag?.PlaceAtScrollPosition(0f);

    }



    public void RefreshContentMetrics()

    {

        if (panelRect == null || contentRect == null)

        {

            return;

        }



        Canvas.ForceUpdateCanvases();
        ApplyStackedContentLayout();
        Canvas.ForceUpdateCanvases();

        float panelHeight = Mathf.Max(panelRect.rect.height, LayoutUtility.GetPreferredHeight(panelRect));
        float layoutContentHeight = contentRect.sizeDelta.y;

        cachedMaxScroll = CalculateMaxScroll(panelHeight);

        float scrollContentHeight = panelHeight + cachedMaxScroll;
        if (scrollContentHeight > layoutContentHeight + 1f)
        {
            contentRect.sizeDelta = new Vector2(0f, scrollContentHeight);
        }



        float maxScroll = GetMaxScroll();

        contentRect.anchoredPosition = new Vector2(

            contentRect.anchoredPosition.x,

            Mathf.Clamp(contentRect.anchoredPosition.y, 0f, maxScroll));



        scrollbarDrag?.RefreshTrackLimits();

        scrollbarDrag?.SyncFromContent();

        EnsureScrollbarVisible();

    }



    private void ApplyStackedContentLayout()
    {
        ProfileArtworksCarousel carousel = GetComponent<ProfileArtworksCarousel>();
        float carouselHeight = carousel != null ? carousel.CardHeight : 380f;
        OtherPanelScrollLayout.Apply(contentRect, carouselHeight);
    }

    private void EnsureViewportMask()

    {

        if (GetComponent<RectMask2D>() == null)

        {

            gameObject.AddComponent<RectMask2D>();

        }

    }



    private void EnsureScrollbarOnPanel()

    {

        if (scrollbarRect == null || panelRect == null)

        {

            return;

        }



        if (scrollbarRect.parent != panelRect)

        {

            scrollbarRect.SetParent(panelRect, false);

        }

    }



    private void EnsureScrollbarVisible()

    {

        if (scrollbarRect != null)

        {

            scrollbarRect.gameObject.SetActive(true);

        }

    }



    private RectTransform FindScrollbar()

    {

        Transform scrollbar = transform.Find(ScrollbarObjectName);

        if (scrollbar == null && contentRect != null)

        {

            scrollbar = contentRect.Find(ScrollbarObjectName);

        }



        return scrollbar as RectTransform;

    }



    private void PrepareScrollbarForDrag()

    {

        Button button = scrollbarRect.GetComponent<Button>();

        if (button != null)

        {

            button.enabled = false;

        }



        scrollbarRect.SetAsLastSibling();

    }



    private void WireScrollbarDrag()

    {

        scrollbarDrag = scrollbarRect.GetComponent<OtherPanelScrollbarDrag>();

        if (scrollbarDrag == null)

        {

            scrollbarDrag = scrollbarRect.gameObject.AddComponent<OtherPanelScrollbarDrag>();

        }



        scrollbarDrag.Setup(panelRect, contentRect, scrollbarRect, TrackPaddingBottom, SeeAllButtonName, () => cachedMaxScroll);

    }



    private float CalculateMaxScroll(float panelHeight)

    {

        float scrollFromPanelSpace = CalculateMaxScrollFromPanelSpace();

        if (scrollFromPanelSpace > 0.5f)

        {

            return scrollFromPanelSpace;

        }



        float scrollFromContentSpace = CalculateMaxScrollFromContentSpace(panelHeight);

        if (scrollFromContentSpace > 0.5f)

        {

            return scrollFromContentSpace;

        }



        return CalculateMaxScrollFromFilteredChildren(panelHeight);

    }



    private float CalculateMaxScrollFromPanelSpace()

    {

        RectTransform boundary = FindScrollBoundaryTarget();

        if (boundary == null)

        {

            return 0f;

        }



        float savedScrollY = contentRect.anchoredPosition.y;

        contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, 0f);

        Canvas.ForceUpdateCanvases();



        Bounds markerInPanel = RectTransformUtility.CalculateRelativeRectTransformBounds(panelRect, boundary);

        float panelBottom = GetVisiblePanelBottomY();

        float maxScroll = (panelBottom + GetEffectiveScrollPadding()) - markerInPanel.min.y;



        contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, savedScrollY);

        return Mathf.Max(0f, maxScroll);

    }



    private float CalculateMaxScrollFromContentSpace(float panelHeight)

    {

        float boundaryBottom = MeasureScrollBoundaryBottom();

        if (boundaryBottom <= 0f)

        {

            return 0f;

        }



        return Mathf.Max(0f, boundaryBottom - panelHeight + GetEffectiveScrollPadding());

    }



    private float CalculateMaxScrollFromFilteredChildren(float panelHeight)

    {

        float minY = float.PositiveInfinity;



        for (int i = 0; i < contentRect.childCount; i++)

        {

            RectTransform child = contentRect.GetChild(i) as RectTransform;

            if (child == null || !child.gameObject.activeInHierarchy || ShouldIgnoreContentChild(child.name))

            {

                continue;

            }



            if (child == contentRect.Find("ArtworksScrollViewport") as RectTransform)

            {

                continue;

            }



            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(contentRect, child);

            minY = Mathf.Min(minY, bounds.min.y);

        }



        if (float.IsPositiveInfinity(minY))

        {

            return 0f;

        }



        float boundaryBottom = minY < 0f ? -minY : minY;

        return Mathf.Max(0f, boundaryBottom - panelHeight + GetEffectiveScrollPadding());

    }



    private float MeasureScrollBoundaryBottom()
    {
        float boundaryBottom = 0f;

        RectTransform marker = FindScrollBoundaryMarker();
        if (marker != null)
        {
            boundaryBottom = GetBottomExtentFromContentTop(
                RectTransformUtility.CalculateRelativeRectTransformBounds(contentRect, marker));
        }

        RectTransform userNotes = FindUserNotes();
        if (userNotes != null)
        {
            float notesBottom = GetBottomExtentFromContentTop(
                RectTransformUtility.CalculateRelativeRectTransformBounds(contentRect, userNotes));
            boundaryBottom = Mathf.Max(boundaryBottom, notesBottom);
        }

        return boundaryBottom;
    }



    private RectTransform FindScrollBoundaryTarget()
    {
        RectTransform marker = FindScrollBoundaryMarker();
        if (marker != null)
        {
            return marker;
        }

        return FindUserNotes();
    }



    private RectTransform FindUserNotes()

    {

        if (contentRect == null)

        {

            return null;

        }



        Transform userNotes = contentRect.Find(UserNotesSectionName);

        if (userNotes != null)

        {

            return userNotes as RectTransform;

        }



        Transform[] children = contentRect.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)

        {

            if (children[i] != null && children[i].name == UserNotesSectionName)

            {

                return children[i] as RectTransform;

            }

        }



        return null;

    }



    private RectTransform FindScrollBoundaryMarker()

    {

        RectTransform userNotes = FindUserNotes();

        if (userNotes == null)

        {

            return null;

        }



        Transform marker = userNotes.Find(NotesScrollBoundaryMarkerName);

        if (marker != null)

        {

            return marker as RectTransform;

        }



        Transform[] children = userNotes.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)

        {

            if (children[i] != null && children[i].name == NotesScrollBoundaryMarkerName)

            {

                return children[i] as RectTransform;

            }

        }



        return null;

    }



    private static bool ShouldIgnoreContentChild(string childName)

    {

        for (int i = 0; i < IgnoredContentChildNames.Length; i++)

        {

            if (IgnoredContentChildNames[i] == childName)

            {

                return true;

            }

        }



        return false;

    }



    private static float GetBottomExtentFromContentTop(Bounds localBounds)

    {

        float bottomEdge = localBounds.min.y;

        return bottomEdge < 0f ? -bottomEdge : bottomEdge;

    }



    private float GetMaxScroll()

    {

        return cachedMaxScroll;

    }

}



/// <summary>

/// Drag the scrollbar along a track that starts below See All (top of OtherPanel area).

/// </summary>

[DisallowMultipleComponent]

public class OtherPanelScrollbarDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler

{

    private const float GapBelowSeeAll = 10f;

    private const float TrackPaddingTopFallback = 12f;



    private RectTransform panel;

    private RectTransform content;

    private RectTransform handle;

    private string seeAllButtonName = "See All";

    private float trackPaddingBottom = 12f;

    private float trackTop;

    private float trackBottom;

    private System.Func<float> resolveMaxScroll;



    public void Setup(

        RectTransform panelRect,

        RectTransform contentRect,

        RectTransform handleRect,

        float paddingBottom,

        string seeAllName,

        System.Func<float> maxScrollResolver = null)

    {

        panel = panelRect;

        content = contentRect;

        handle = handleRect;

        trackPaddingBottom = paddingBottom;

        seeAllButtonName = string.IsNullOrEmpty(seeAllName) ? "See All" : seeAllName;

        resolveMaxScroll = maxScrollResolver;

        RefreshTrackLimits();

        PlaceAtScrollPosition(0f);

    }



    public void RefreshTrackLimits()

    {

        if (panel == null || handle == null)

        {

            return;

        }



        Canvas.ForceUpdateCanvases();

        trackBottom = GetTrackBottom();

        trackTop = ResolveTrackTopBelowSeeAll();

        if (trackTop <= trackBottom)

        {

            trackTop = trackBottom + handle.rect.height + 4f;

        }

    }



    public void PlaceAtScrollPosition(float scrollT)

    {

        if (handle == null)

        {

            return;

        }



        RefreshTrackLimits();

        float travel = Mathf.Max(0f, trackTop - trackBottom);

        float y = trackTop - Mathf.Clamp01(scrollT) * travel;

        Vector2 position = handle.anchoredPosition;

        position.y = y;

        handle.anchoredPosition = position;

    }



    public void SyncFromContent()

    {

        if (handle == null || content == null || panel == null)

        {

            return;

        }



        float maxScroll = GetMaxScroll();

        float scrollT = maxScroll <= 0.01f ? 0f : Mathf.Clamp01(content.anchoredPosition.y / maxScroll);

        PlaceAtScrollPosition(scrollT);

    }



    public void OnBeginDrag(PointerEventData eventData)

    {

        RefreshTrackLimits();

    }



    public void OnDrag(PointerEventData eventData)

    {

        if (handle == null || content == null || panel == null)

        {

            return;

        }



        float travel = trackTop - trackBottom;

        if (travel <= 0.01f)

        {

            return;

        }



        float maxScroll = GetMaxScroll();

        if (maxScroll <= 0.01f)

        {

            return;

        }



        Vector2 position = handle.anchoredPosition;

        position.y = Mathf.Clamp(position.y + eventData.delta.y, trackBottom, trackTop);

        handle.anchoredPosition = position;



        float scrollT = (trackTop - position.y) / travel;

        content.anchoredPosition = new Vector2(content.anchoredPosition.x, scrollT * maxScroll);

    }



    public void OnEndDrag(PointerEventData eventData)

    {

    }



    private float ResolveTrackTopBelowSeeAll()

    {

        float fallbackTop = panel.rect.height * 0.5f - TrackPaddingTopFallback - handle.rect.height * 0.5f;

        RectTransform seeAll = FindSeeAllRect();

        if (seeAll == null || !seeAll.gameObject.activeInHierarchy)

        {

            return fallbackTop;

        }



        Bounds seeAllBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, seeAll);

        return seeAllBounds.min.y - GapBelowSeeAll - handle.rect.height * 0.5f;

    }



    private RectTransform FindSeeAllRect()

    {

        if (content == null)

        {

            return null;

        }



        Transform seeAll = content.Find(seeAllButtonName);

        if (seeAll != null)

        {

            return seeAll as RectTransform;

        }



        for (int i = 0; i < content.childCount; i++)

        {

            Transform child = content.GetChild(i);

            if (child != null && child.name == seeAllButtonName)

            {

                return child as RectTransform;

            }

        }



        return null;

    }



    private float GetMaxScroll()

    {

        if (resolveMaxScroll != null)

        {

            return Mathf.Max(0f, resolveMaxScroll());

        }



        Canvas.ForceUpdateCanvases();

        return Mathf.Max(0f, content.rect.height - panel.rect.height);

    }



    private float GetTrackBottom()

    {

        return -panel.rect.height * 0.5f + trackPaddingBottom + handle.rect.height * 0.5f;

    }

}


