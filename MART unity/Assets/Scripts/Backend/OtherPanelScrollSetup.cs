using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds vertical scroll + scrollbar to OtherPanel so all panel content can be scrolled.
/// </summary>
public static class OtherPanelScrollSetup
{
    public const string ScrollRootName = "OtherPanelScroll";
    public const string ViewportName = "OtherPanelScrollViewport";
    public const string ContentName = "OtherPanelContent";
    public const string HandleName = "OtherPanelScrollHandle";

    private const string OtherPanelName = "OtherPanel";
    private const float ScrollbarWidth = 28f;
    private const float DefaultSectionHeight = 120f;
    private const float ArtworksViewportHeight = 430f;

    public static void Ensure(bool registerUndo = false)
    {
        GameObject otherPanel = GameObject.Find(OtherPanelName);
        if (otherPanel == null)
        {
            return;
        }

        Transform scrollRoot = otherPanel.transform.Find(ScrollRootName);
        if (scrollRoot == null)
        {
            scrollRoot = BuildScrollHierarchy(otherPanel.transform, registerUndo);
        }
        else
        {
            Transform content = scrollRoot.Find(ViewportName + "/" + ContentName);
            if (content == null)
            {
                content = scrollRoot.Find(ContentName);
            }

            if (content != null)
            {
                MovePanelChildrenIntoContent(otherPanel.transform, content, registerUndo);
            }
        }

        if (scrollRoot == null)
        {
            return;
        }

        WireScrollDrivers(scrollRoot);
    }

    public static void RefreshLayout()
    {
        GameObject otherPanel = GameObject.Find(OtherPanelName);
        if (otherPanel == null)
        {
            return;
        }

        Transform scrollRoot = otherPanel.transform.Find(ScrollRootName);
        if (scrollRoot == null)
        {
            return;
        }

        WireScrollDrivers(scrollRoot);
    }

    private static Transform BuildScrollHierarchy(Transform otherPanel, bool registerUndo)
    {
        var scrollObject = new GameObject(ScrollRootName, typeof(RectTransform));
        RegisterCreated(scrollObject, registerUndo);
        SetParent(scrollObject.transform, otherPanel, registerUndo, "OtherPanel scroll root");
        scrollObject.transform.SetAsFirstSibling();
        StretchFull(scrollObject.GetComponent<RectTransform>());

        var viewportObject = new GameObject(
            ViewportName,
            typeof(RectTransform),
            typeof(RectMask2D),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(SimpleVerticalDragScroll));
        RegisterCreated(viewportObject, registerUndo);
        SetParent(viewportObject.transform, scrollObject.transform, registerUndo, "OtherPanel scroll viewport");
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        StretchFull(viewportRect);
        viewportRect.offsetMax = new Vector2(-ScrollbarWidth, 0f);

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
        viewportImage.raycastTarget = true;

        var contentObject = new GameObject(
            ContentName,
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        RegisterCreated(contentObject, registerUndo);
        SetParent(contentObject.transform, viewportObject.transform, registerUndo, "OtherPanel scroll content");
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(12, 12, 16, 24);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var handleObject = new GameObject(
            HandleName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        RegisterCreated(handleObject, registerUndo);
        SetParent(handleObject.transform, scrollObject.transform, registerUndo, "OtherPanel scroll handle");
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(1f, 0f);
        handleRect.anchorMax = new Vector2(1f, 1f);
        handleRect.pivot = new Vector2(1f, 1f);
        handleRect.anchoredPosition = new Vector2(-6f, 0f);
        handleRect.sizeDelta = new Vector2(18f, -24f);
        handleRect.offsetMin = new Vector2(-ScrollbarWidth, 12f);
        handleRect.offsetMax = new Vector2(-6f, -12f);

        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = new Color(0.82f, 0.82f, 0.82f, 1f);
        handleImage.raycastTarget = true;

        Button handleButton = handleObject.GetComponent<Button>();
        handleButton.transition = Selectable.Transition.None;

        MovePanelChildrenIntoContent(otherPanel, contentObject.transform, registerUndo);

        Image panelImage = otherPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.raycastTarget = false;
        }

        return scrollObject.transform;
    }

    private static void MovePanelChildrenIntoContent(Transform otherPanel, Transform content, bool registerUndo)
    {
        var children = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < otherPanel.childCount; i++)
        {
            Transform child = otherPanel.GetChild(i);
            if (child == null || child == content.parent || child == content)
            {
                continue;
            }

            if (child.name == ScrollRootName)
            {
                continue;
            }

            children.Add(child);
        }

        for (int i = 0; i < children.Count; i++)
        {
            Transform child = children[i];
            SetParent(child, content, registerUndo, "Move into OtherPanel content");
            PrepareChildForVerticalLayout(child as RectTransform, registerUndo);
        }
    }

    private static void PrepareChildForVerticalLayout(RectTransform rect, bool registerUndo)
    {
        if (rect == null)
        {
            return;
        }

        float height = ResolvePreferredHeight(rect);

        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;

        if (rect.sizeDelta.x <= 0f)
        {
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        }
        else
        {
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        }

        LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = AddComponent<LayoutElement>(rect.gameObject, registerUndo);
        }

        layoutElement.preferredHeight = height;
        layoutElement.minHeight = height;
        layoutElement.flexibleHeight = 0f;
        layoutElement.flexibleWidth = 0f;
    }

    private static float ResolvePreferredHeight(RectTransform rect)
    {
        if (rect.name == "ArtworksScrollViewport")
        {
            return ArtworksViewportHeight;
        }

        if (rect.sizeDelta.y > 1f)
        {
            return rect.sizeDelta.y;
        }

        if (rect.rect.height > 1f)
        {
            return rect.rect.height;
        }

        return DefaultSectionHeight;
    }

    private static void WireScrollDrivers(Transform scrollRoot)
    {
        if (scrollRoot == null)
        {
            return;
        }

        RectTransform viewport = scrollRoot.Find(ViewportName) as RectTransform;
        RectTransform content = viewport != null ? viewport.Find(ContentName) as RectTransform : null;
        RectTransform handle = scrollRoot.Find(HandleName) as RectTransform;

        if (viewport == null || content == null || handle == null)
        {
            return;
        }

        ScrollRect legacyScroll = scrollRoot.GetComponentInParent<ScrollRect>();
        if (legacyScroll != null && legacyScroll.gameObject.name == OtherPanelName)
        {
            legacyScroll.enabled = false;
        }

        SimpleVerticalDragScroll dragScroll = viewport.GetComponent<SimpleVerticalDragScroll>();
        if (dragScroll == null)
        {
            dragScroll = AddComponent<SimpleVerticalDragScroll>(viewport.gameObject, false);
        }

        dragScroll.Setup(viewport, content);

        GalleryVerticalScrollHandle handleDriver = handle.GetComponent<GalleryVerticalScrollHandle>();
        if (handleDriver == null)
        {
            handleDriver = AddComponent<GalleryVerticalScrollHandle>(handle.gameObject, false);
        }

        handleDriver.Setup(viewport, content, handle, scrollRoot as RectTransform);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        handleDriver.SyncAfterContentResize();
        dragScroll.ResetToTop();
    }

    private static void StretchFull(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetParent(Transform child, Transform parent, bool registerUndo, string undoName)
    {
        if (registerUndo)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.Undo.SetTransformParent(child, parent, undoName);
                return;
            }
#endif
        }

        child.SetParent(parent, false);
    }

    private static void RegisterCreated(Object created, bool registerUndo)
    {
#if UNITY_EDITOR
        if (registerUndo && created != null && !Application.isPlaying)
        {
            UnityEditor.Undo.RegisterCreatedObjectUndo(created, "OtherPanel scroll");
        }
#endif
    }

    private static T AddComponent<T>(GameObject target, bool registerUndo) where T : Component
    {
#if UNITY_EDITOR
        if (registerUndo && !Application.isPlaying)
        {
            return UnityEditor.Undo.AddComponent<T>(target);
        }
#endif
        return target.GetComponent<T>() ?? target.AddComponent<T>();
    }
}
