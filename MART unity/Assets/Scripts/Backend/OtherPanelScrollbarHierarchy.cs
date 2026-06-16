using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the persistent OtherPanel scroll hierarchy (content container + scrollbar wiring).
/// </summary>
public static class OtherPanelScrollbarHierarchy
{
    public static bool IsBaked(Transform otherPanel)
    {
        return otherPanel != null &&
               otherPanel.Find(OtherPanelScrollbarController.ScrollContentName) != null;
    }

    public static bool Bake(Transform otherPanel, bool registerUndo = false)
    {
        if (otherPanel == null)
        {
            return false;
        }

        RectTransform panelRect = otherPanel as RectTransform;
        if (panelRect == null)
        {
            return false;
        }

        RectTransform contentRect = EnsureScrollContent(panelRect, registerUndo);
        if (contentRect == null)
        {
            return false;
        }

        ReparentScrollableChildren(panelRect, contentRect, registerUndo);
        EnsurePanelComponents(panelRect.gameObject, registerUndo);
        EnsureScrollbarComponents(panelRect, registerUndo);
        return true;
    }

    private static RectTransform EnsureScrollContent(RectTransform panelRect, bool registerUndo)
    {
        Transform existing = panelRect.Find(OtherPanelScrollbarController.ScrollContentName);
        if (existing == null)
        {
            var contentObject = new GameObject(OtherPanelScrollbarController.ScrollContentName, typeof(RectTransform));
            RegisterCreated(contentObject, registerUndo);
            SetParent(contentObject.transform, panelRect, registerUndo, "Create OtherPanel scroll content");
            contentObject.transform.SetAsFirstSibling();
            existing = contentObject.transform;
        }

        RectTransform content = existing as RectTransform;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 1200f);
        return content;
    }

    private static void ReparentScrollableChildren(RectTransform panelRect, RectTransform content, bool registerUndo)
    {
        var children = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < panelRect.childCount; i++)
        {
            Transform child = panelRect.GetChild(i);
            if (child == null || child == content)
            {
                continue;
            }

            if (child.name == OtherPanelScrollbarController.ScrollbarObjectName ||
                child.name == OtherPanelScrollbarController.ScrollContentName)
            {
                continue;
            }

            children.Add(child);
        }

        for (int i = 0; i < children.Count; i++)
        {
            SetParent(children[i], content, registerUndo, "Move into OtherPanel scroll content");
        }

        EnsureScrollbarOnPanel(panelRect, registerUndo);
    }

    private static void EnsureScrollbarOnPanel(RectTransform panelRect, bool registerUndo)
    {
        Transform scrollbar = FindScrollbarTransform(panelRect);
        if (scrollbar == null)
        {
            return;
        }

        if (scrollbar.parent != panelRect)
        {
            SetParent(scrollbar, panelRect, registerUndo, "Keep scrollbar fixed on OtherPanel");
        }

        scrollbar.SetAsLastSibling();
    }

    private static Transform FindScrollbarTransform(Transform otherPanel)
    {
        Transform scrollbar = otherPanel.Find(OtherPanelScrollbarController.ScrollbarObjectName);
        if (scrollbar != null)
        {
            return scrollbar;
        }

        Transform content = otherPanel.Find(OtherPanelScrollbarController.ScrollContentName);
        return content != null ? content.Find(OtherPanelScrollbarController.ScrollbarObjectName) : null;
    }

    private static void EnsurePanelComponents(GameObject panel, bool registerUndo)
    {
        if (panel.GetComponent<RectMask2D>() == null)
        {
            AddComponent<RectMask2D>(panel, registerUndo);
        }

        if (panel.GetComponent<OtherPanelScrollbarController>() == null)
        {
            AddComponent<OtherPanelScrollbarController>(panel, registerUndo);
        }
    }

    private static void EnsureScrollbarComponents(RectTransform panelRect, bool registerUndo)
    {
        EnsureScrollbarOnPanel(panelRect, registerUndo);
        Transform scrollbar = FindScrollbarTransform(panelRect);
        if (scrollbar == null)
        {
            return;
        }

        Button button = scrollbar.GetComponent<Button>();
        if (button != null)
        {
            button.enabled = false;
        }

        if (scrollbar.GetComponent<OtherPanelScrollbarDrag>() == null)
        {
            AddComponent<OtherPanelScrollbarDrag>(scrollbar.gameObject, registerUndo);
        }
    }

    private static void SetParent(Transform child, Transform parent, bool registerUndo, string undoName)
    {
#if UNITY_EDITOR
        if (registerUndo && !Application.isPlaying)
        {
            UnityEditor.Undo.SetTransformParent(child, parent, undoName);
            return;
        }
#endif
        child.SetParent(parent, false);
    }

    private static void RegisterCreated(Object created, bool registerUndo)
    {
#if UNITY_EDITOR
        if (registerUndo && created != null && !Application.isPlaying)
        {
            UnityEditor.Undo.RegisterCreatedObjectUndo(created, "OtherPanel scroll hierarchy");
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
