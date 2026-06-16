using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the vertical scroll gallery UI under the User's work full-screen panel.
/// </summary>
public static class UserWorksGalleryBuilder
{
    private const string GalleryRootName = "UserWorksGalleryRoot";
    private const string ScrollContainerName = "scroll";
    private const string ScrollViewportName = "ArtworksScrollViewport";
    private const string ArtworksListName = "ArtworksList";
    private const string ScrollHandleName = "ScrollHandle";

    public static ProfileArtworksGallery Ensure(GameObject userWorksScreen)
    {
        if (userWorksScreen == null)
        {
            return null;
        }

        Transform galleryRoot = userWorksScreen.transform.Find(GalleryRootName);
        if (galleryRoot == null)
        {
            GameObject rootObject = new GameObject(GalleryRootName, typeof(RectTransform));
            galleryRoot = rootObject.transform;
            galleryRoot.SetParent(userWorksScreen.transform, false);
            StretchRect(galleryRoot as RectTransform, 80f, 0f);
        }

        EnsureScrollHierarchy(galleryRoot);

        ProfileArtworksGallery gallery = userWorksScreen.GetComponent<ProfileArtworksGallery>();
        if (gallery == null)
        {
            gallery = userWorksScreen.AddComponent<ProfileArtworksGallery>();
        }

        return gallery;
    }

    private static void EnsureScrollHierarchy(Transform galleryRoot)
    {
        Transform scrollContainer = galleryRoot.Find(ScrollContainerName);
        if (scrollContainer == null)
        {
            GameObject scrollObject = new GameObject(ScrollContainerName, typeof(RectTransform));
            scrollContainer = scrollObject.transform;
            scrollContainer.SetParent(galleryRoot, false);
            StretchRect(scrollContainer as RectTransform, 0f, 0f);
        }
        else
        {
            StretchRect(scrollContainer as RectTransform, 0f, 0f);
        }

        Transform viewport = scrollContainer.Find(ScrollViewportName);
        if (viewport == null)
        {
            GameObject viewportObject = new GameObject(ScrollViewportName, typeof(RectTransform), typeof(RectMask2D));
            viewport = viewportObject.transform;
            viewport.SetParent(scrollContainer, false);
            StretchRect(viewport as RectTransform, 0f, 0f);
            viewportObject.GetComponent<RectTransform>().offsetMax = new Vector2(-40f, 0f);

            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = false;
        }

        Transform list = viewport.Find(ArtworksListName);
        if (list == null)
        {
            GameObject listObject = new GameObject(ArtworksListName, typeof(RectTransform));
            list = listObject.transform;
            list.SetParent(viewport, false);

            RectTransform listRect = listObject.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = Vector2.zero;
        }

        Transform handle = scrollContainer.Find(ScrollHandleName);
        if (handle == null)
        {
            Transform legacyHandle = scrollContainer.Find("scroll");
            if (legacyHandle != null && legacyHandle != scrollContainer)
            {
                legacyHandle.name = ScrollHandleName;
                handle = legacyHandle;
            }
        }

        if (handle == null)
        {
            GameObject handleObject = new GameObject(ScrollHandleName, typeof(RectTransform), typeof(Image), typeof(Button));
            handle = handleObject.transform;
            handle.SetParent(scrollContainer, false);

            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(1f, 1f);
            handleRect.anchorMax = new Vector2(1f, 1f);
            handleRect.pivot = new Vector2(1f, 1f);
            handleRect.sizeDelta = new Vector2(22f, 120f);
            handleRect.anchoredPosition = new Vector2(-8f, 0f);

            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.color = new Color(0.82f, 0.82f, 0.82f, 1f);
            handleImage.raycastTarget = true;

            Button handleButton = handleObject.GetComponent<Button>();
            handleButton.transition = Selectable.Transition.None;
        }
    }

    private static void StretchRect(RectTransform rect, float bottomInset, float topInset)
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
        rect.offsetMin = new Vector2(0f, bottomInset);
        rect.offsetMax = new Vector2(0f, -topInset);
    }
}
