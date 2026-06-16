using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Fullscreen overlay that displays a tapped AR photo at its full aspect ratio.
/// Lives directly under the active <see cref="Canvas"/> so it covers the whole screen.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Museum AR/AR Photo Fullscreen Viewer")]
public class ARPhotoFullscreenViewer : MonoBehaviour
{
    private const string ViewerObjectName = "ARPhotoFullscreenViewer";
    private const string BackdropObjectName = "Backdrop";
    private const string PhotoObjectName = "Photo";
    private const string CloseButtonObjectName = "CloseButton";
    private const string DeleteButtonObjectName = "Delete";

    private static ARPhotoFullscreenViewer instance;

    private RectTransform viewerRect;
    private RectTransform photoRect;
    private RawImage photoImage;
    private Button backdropButton;
    private Button closeButton;
    private Button deleteButton;
    private string currentPhotoPath;

    public static void Show(Texture2D texture, string photoPath = null)
    {
        if (texture == null)
        {
            return;
        }

        ARPhotoFullscreenViewer viewer = GetOrCreate();
        if (viewer == null)
        {
            return;
        }

        viewer.DisplayTexture(texture, photoPath);
    }

    public static void Hide()
    {
        if (instance != null)
        {
            instance.gameObject.SetActive(false);
        }
    }

    private static ARPhotoFullscreenViewer GetOrCreate()
    {
        if (instance != null)
        {
            return instance;
        }

        Canvas canvas = FindHostCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("ARPhotoFullscreenViewer: no Canvas found in active scene.");
            return null;
        }

        Transform existing = canvas.transform.Find(ViewerObjectName);
        GameObject viewerObject;
        if (existing != null)
        {
            viewerObject = existing.gameObject;
        }
        else
        {
            viewerObject = new GameObject(
                ViewerObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            viewerObject.transform.SetParent(canvas.transform, false);
        }

        instance = viewerObject.GetComponent<ARPhotoFullscreenViewer>();
        if (instance == null)
        {
            instance = viewerObject.AddComponent<ARPhotoFullscreenViewer>();
        }

        instance.EnsureHierarchy();
        return instance;
    }

    private static Canvas FindHostCanvas()
    {
        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        Canvas best = null;
        for (int i = 0; i < allCanvases.Length; i++)
        {
            Canvas candidate = allCanvases[i];
            if (candidate == null || candidate.transform.parent != null)
            {
                continue;
            }

            if (best == null || candidate.sortingOrder > best.sortingOrder)
            {
                best = candidate;
            }
        }

        return best;
    }

    private void EnsureHierarchy()
    {
        viewerRect = transform as RectTransform;
        if (viewerRect != null)
        {
            viewerRect.anchorMin = Vector2.zero;
            viewerRect.anchorMax = Vector2.one;
            viewerRect.pivot = new Vector2(0.5f, 0.5f);
            viewerRect.offsetMin = Vector2.zero;
            viewerRect.offsetMax = Vector2.zero;
            viewerRect.localScale = Vector3.one;
            viewerRect.SetAsLastSibling();
        }

        Image rootImage = GetComponent<Image>();
        if (rootImage != null)
        {
            rootImage.color = new Color(0f, 0f, 0f, 0f);
            rootImage.raycastTarget = false;
        }

        Transform backdropTransform = transform.Find(BackdropObjectName);
        GameObject backdropObject;
        if (backdropTransform != null)
        {
            backdropObject = backdropTransform.gameObject;
        }
        else
        {
            backdropObject = new GameObject(
                BackdropObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            backdropObject.transform.SetParent(transform, false);
        }

        RectTransform backdropRect = backdropObject.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.pivot = new Vector2(0.5f, 0.5f);
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        backdropRect.localScale = Vector3.one;

        Image backdropImage = backdropObject.GetComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, 0.92f);
        backdropImage.raycastTarget = true;

        backdropButton = backdropObject.GetComponent<Button>();
        if (backdropButton == null)
        {
            backdropButton = backdropObject.AddComponent<Button>();
        }

        backdropButton.transition = Selectable.Transition.None;
        backdropButton.onClick.RemoveAllListeners();
        backdropButton.onClick.AddListener(Hide);

        Transform photoTransform = transform.Find(PhotoObjectName);
        GameObject photoObject;
        if (photoTransform != null)
        {
            photoObject = photoTransform.gameObject;
        }
        else
        {
            photoObject = new GameObject(
                PhotoObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
                typeof(AspectRatioFitter));
            photoObject.transform.SetParent(transform, false);
        }

        photoRect = photoObject.GetComponent<RectTransform>();
        photoRect.anchorMin = new Vector2(0.1f, 0.1f);
        photoRect.anchorMax = new Vector2(0.9f, 0.9f);
        photoRect.pivot = new Vector2(0.5f, 0.5f);
        photoRect.offsetMin = Vector2.zero;
        photoRect.offsetMax = Vector2.zero;
        photoRect.localScale = Vector3.one;

        photoImage = photoObject.GetComponent<RawImage>();
        photoImage.raycastTarget = false;
        photoImage.uvRect = new Rect(0f, 0f, 1f, 1f);

        AspectRatioFitter aspectFitter = photoObject.GetComponent<AspectRatioFitter>();
        if (aspectFitter == null)
        {
            aspectFitter = photoObject.AddComponent<AspectRatioFitter>();
        }

        aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        Transform closeButtonTransform = transform.Find(CloseButtonObjectName);
        GameObject closeButtonObject;
        if (closeButtonTransform != null)
        {
            closeButtonObject = closeButtonTransform.gameObject;
        }
        else
        {
            closeButtonObject = new GameObject(
                CloseButtonObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            closeButtonObject.transform.SetParent(transform, false);

            CreateCloseButtonGlyph(closeButtonObject.transform);
        }

        RectTransform closeButtonRect = closeButtonObject.GetComponent<RectTransform>();
        closeButtonRect.anchorMin = new Vector2(1f, 1f);
        closeButtonRect.anchorMax = new Vector2(1f, 1f);
        closeButtonRect.pivot = new Vector2(1f, 1f);
        closeButtonRect.anchoredPosition = new Vector2(-40f, -60f);
        closeButtonRect.sizeDelta = new Vector2(96f, 96f);
        closeButtonRect.localScale = Vector3.one;

        Image closeBackground = closeButtonObject.GetComponent<Image>();
        closeBackground.color = new Color(1f, 1f, 1f, 0.18f);
        closeBackground.raycastTarget = true;

        closeButton = closeButtonObject.GetComponent<Button>();
        if (closeButton == null)
        {
            closeButton = closeButtonObject.AddComponent<Button>();
        }

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(Hide);

        Transform deleteButtonTransform = transform.Find(DeleteButtonObjectName);
        if (deleteButtonTransform != null)
        {
            deleteButton = deleteButtonTransform.GetComponent<Button>();
            if (deleteButton == null)
            {
                deleteButton = deleteButtonTransform.gameObject.AddComponent<Button>();
            }

            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }

        gameObject.SetActive(false);
    }

    private void OnDeleteClicked()
    {
        if (string.IsNullOrWhiteSpace(currentPhotoPath))
        {
            Debug.LogWarning("ARPhotoFullscreenViewer: cannot delete photo without a file path.");
            Hide();
            ARPhotoGallery.ShowGalleryScreen();
            return;
        }

        string pathToDelete = currentPhotoPath;
        currentPhotoPath = null;

        if (photoImage != null)
        {
            photoImage.texture = null;
        }

        Hide();
        ARPhotoLibrary.DeletePhoto(pathToDelete);
        ARPhotoGallery.ShowGalleryScreen();
    }

    private static void CreateCloseButtonGlyph(Transform parent)
    {
        for (int i = 0; i < 2; i++)
        {
            var bar = new GameObject(
                "Bar",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            bar.transform.SetParent(parent, false);

            RectTransform barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 0.5f);
            barRect.anchorMax = new Vector2(0.5f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.sizeDelta = new Vector2(54f, 6f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 45f : -45f);
            barRect.localScale = Vector3.one;

            Image barImage = bar.GetComponent<Image>();
            barImage.color = Color.white;
            barImage.raycastTarget = false;
        }
    }

    private void DisplayTexture(Texture2D texture, string photoPath)
    {
        if (photoImage == null || photoRect == null)
        {
            EnsureHierarchy();
        }

        if (photoImage == null)
        {
            return;
        }

        currentPhotoPath = photoPath;
        photoImage.texture = texture;

        AspectRatioFitter aspectFitter = photoImage.GetComponent<AspectRatioFitter>();
        if (aspectFitter != null && texture != null && texture.height > 0)
        {
            aspectFitter.aspectRatio = (float)texture.width / texture.height;
        }

        gameObject.SetActive(true);

        if (viewerRect != null)
        {
            viewerRect.SetAsLastSibling();
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
