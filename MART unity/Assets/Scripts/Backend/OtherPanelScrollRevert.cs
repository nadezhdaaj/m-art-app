using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Restores OtherPanel layout after experimental scroll setup broke hierarchy positions.
/// </summary>
public static class OtherPanelScrollRevert
{
    private const string OtherPanelName = "OtherPanel";
    private const string ScrollRootName = OtherPanelScrollSetup.ScrollRootName;
    private const string ContentName = OtherPanelScrollSetup.ContentName;

    private static readonly Dictionary<string, RectRestoreData> OriginalRects = new Dictionary<string, RectRestoreData>
    {
        ["Design elements"] = new RectRestoreData(
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-92.1497f, -11.341f),
            new Vector2(799.0405f, 799.0353f)),
        ["ArtworksScrollViewport"] = new RectRestoreData(
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero),
        ["See All"] = new RectRestoreData(
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(170f, -775f),
            new Vector2(350.4779f, 110.2f)),
        ["carousel"] = new RectRestoreData(
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(393f, -28f),
            new Vector2(114.4968f, 37.5877f)),
    };

    private static readonly string[] OriginalChildOrder =
    {
        "Design elements",
        "ArtworksScrollViewport",
        "See All",
        "carousel",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoRestoreOnMainStage()
    {
        if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("main stage"))
        {
            return;
        }

        Restore();
    }

    public static void Restore()
    {
        GameObject otherPanel = GameObject.Find(OtherPanelName);
        if (otherPanel == null)
        {
            return;
        }

        if (OtherPanelScrollbarHierarchy.IsBaked(otherPanel.transform))
        {
            CleanupDuplicateSeeAllButtons(otherPanel.transform);
            return;
        }

        Transform scrollRoot = otherPanel.transform.Find(ScrollRootName);
        Transform content = scrollRoot != null ? scrollRoot.Find(OtherPanelScrollSetup.ViewportName + "/" + ContentName) : null;
        if (content == null && scrollRoot != null)
        {
            content = scrollRoot.Find(ContentName);
        }

        if (content != null)
        {
            UnwrapContentChildren(otherPanel.transform, content);
        }

        RemoveScrollHierarchy(otherPanel.transform);
        RestoreOriginalChildren(otherPanel.transform);
        CleanupDuplicateSeeAllButtons(otherPanel.transform);

        Image panelImage = otherPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.raycastTarget = false;
        }
    }

    private static void UnwrapContentChildren(Transform otherPanel, Transform content)
    {
        var moved = new List<Transform>();
        for (int i = 0; i < content.childCount; i++)
        {
            moved.Add(content.GetChild(i));
        }

        for (int i = 0; i < moved.Count; i++)
        {
            Transform child = moved[i];
            if (child == null)
            {
                continue;
            }

            child.SetParent(otherPanel, false);
            RemoveLayoutElement(child.gameObject);
        }
    }

    private static void RestoreOriginalChildren(Transform otherPanel)
    {
        for (int i = 0; i < OriginalChildOrder.Length; i++)
        {
            string childName = OriginalChildOrder[i];
            Transform child = otherPanel.Find(childName);
            if (child == null)
            {
                continue;
            }

            child.SetParent(otherPanel, false);
            child.gameObject.SetActive(true);
            RemoveLayoutElement(child.gameObject);

            if (OriginalRects.TryGetValue(childName, out RectRestoreData data))
            {
                ApplyRect(child as RectTransform, data);
            }
        }

        for (int i = 0; i < OriginalChildOrder.Length; i++)
        {
            Transform child = otherPanel.Find(OriginalChildOrder[i]);
            if (child != null)
            {
                child.SetSiblingIndex(i);
            }
        }
    }

    private static void CleanupDuplicateSeeAllButtons(Transform otherPanel)
    {
        int keptSeeAll = 0;
        for (int i = 0; i < otherPanel.childCount; i++)
        {
            Transform child = otherPanel.GetChild(i);
            if (child == null || child.name != "See All")
            {
                continue;
            }

            if (keptSeeAll == 0 && child.gameObject.layer == 5)
            {
                keptSeeAll++;
                continue;
            }

            child.gameObject.SetActive(false);
        }
    }

    private static void RemoveScrollHierarchy(Transform otherPanel)
    {
        Transform scrollRoot = otherPanel.Find(ScrollRootName);
        if (scrollRoot == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(scrollRoot.gameObject);
        }
        else
        {
            Object.DestroyImmediate(scrollRoot.gameObject);
        }
    }

    private static void RemoveLayoutElement(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(layoutElement);
        }
        else
        {
            Object.DestroyImmediate(layoutElement);
        }
    }

    private static void ApplyRect(RectTransform rect, RectRestoreData data)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = data.AnchorMin;
        rect.anchorMax = data.AnchorMax;
        rect.pivot = data.Pivot;
        rect.anchoredPosition = data.AnchoredPosition;
        rect.sizeDelta = data.SizeDelta;
        rect.localScale = Vector3.one;
    }

    private readonly struct RectRestoreData
    {
        public readonly Vector2 AnchorMin;
        public readonly Vector2 AnchorMax;
        public readonly Vector2 Pivot;
        public readonly Vector2 AnchoredPosition;
        public readonly Vector2 SizeDelta;

        public RectRestoreData(
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            AnchorMin = anchorMin;
            AnchorMax = anchorMax;
            Pivot = pivot;
            AnchoredPosition = anchoredPosition;
            SizeDelta = sizeDelta;
        }
    }
}
