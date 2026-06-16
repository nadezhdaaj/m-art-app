using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ArtworkCardView : MonoBehaviour
{
    [SerializeField] private RawImage previewImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button openButton;

    private ArtworkDto artwork;
    private Action<ArtworkDto> onSelected;
    private Action<ArtworkDto> onDelete;
    private string pendingPreviewUrl;
    private Coroutine previewLoadCoroutine;
    private GameObject deleteOverlay;
    private bool deleteMode;

    private void Awake()
    {
        ResolveReferences();
        EnsureClickTarget();
    }

    private void OnEnable()
    {
        TryStartPreviewLoad();
    }

    private void OnDisable()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(HandleSelected);
        }

        if (previewLoadCoroutine != null)
        {
            StopCoroutine(previewLoadCoroutine);
            previewLoadCoroutine = null;
        }
    }

    public void Bind(ArtworkDto value, Action<ArtworkDto> onSelectedCallback)
    {
        artwork = value;
        onSelected = onSelectedCallback;
        ResolveReferences();
        EnsureClickTarget();

        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(value?.title) ? "Без названия" : value.title;
            titleText.raycastTarget = false;
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.IsNullOrWhiteSpace(value?.description)
                ? "Нажмите, чтобы открыть и дополнить работу."
                : value.description;
            descriptionText.raycastTarget = false;
        }

        if (openButton != null)
        {
            openButton.onClick.RemoveListener(HandleSelected);
            openButton.onClick.AddListener(HandleSelected);
            openButton.interactable = true;
        }

        if (previewImage != null)
        {
            previewImage.raycastTarget = false;
        }

        pendingPreviewUrl = null;
        if (previewImage != null && !string.IsNullOrWhiteSpace(value?.thumbnailUrl ?? value?.imageUrl))
        {
            pendingPreviewUrl = ResolveArtworkImageUrl(value.thumbnailUrl ?? value.imageUrl);
        }

        TryStartPreviewLoad();
    }

    private void ResolveReferences()
    {
        if (previewImage == null)
        {
            previewImage = transform.Find("Preview")?.GetComponent<RawImage>();
        }

        if (titleText == null)
        {
            titleText = transform.Find("Title")?.GetComponent<TMP_Text>();
        }

        if (descriptionText == null)
        {
            descriptionText = transform.Find("Description")?.GetComponent<TMP_Text>();
        }

        if (openButton == null)
        {
            openButton = GetComponent<Button>();
        }
    }

    private void EnsureClickTarget()
    {
        Image background = GetComponent<Image>();
        if (background != null)
        {
            background.raycastTarget = true;
        }

        if (openButton == null)
        {
            openButton = gameObject.AddComponent<Button>();
            openButton.transition = Selectable.Transition.None;
            if (background != null)
            {
                openButton.targetGraphic = background;
            }
        }
    }

    private void TryStartPreviewLoad()
    {
        if (string.IsNullOrWhiteSpace(pendingPreviewUrl) || previewImage == null)
        {
            return;
        }

        if (!isActiveAndEnabled)
        {
            return;
        }

        if (previewLoadCoroutine != null)
        {
            StopCoroutine(previewLoadCoroutine);
        }

        previewLoadCoroutine = StartCoroutine(LoadPreview(pendingPreviewUrl));
    }

    private void HandleSelected()
    {
        if (artwork == null || deleteMode)
        {
            return;
        }

        onSelected?.Invoke(artwork);
    }

    public void SetupDelete(Action<ArtworkDto> onDeleteCallback)
    {
        onDelete = onDeleteCallback;
    }

    public void SetDeleteMode(bool active)
    {
        deleteMode = active;
        EnsureDeleteOverlay();

        if (deleteOverlay != null)
        {
            deleteOverlay.transform.SetAsLastSibling();
            deleteOverlay.SetActive(active);
        }

        if (openButton != null)
        {
            openButton.interactable = !active && artwork != null;
        }
    }

    private void EnsureDeleteOverlay()
    {
        if (deleteOverlay != null)
        {
            return;
        }

        GameObject overlayObject = new GameObject("DeleteOverlay",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        overlayObject.transform.SetParent(transform, false);

        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.08f);
        rect.anchorMax = new Vector2(0.92f, 0.92f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image background = overlayObject.GetComponent<Image>();
        background.color = new Color(0.85f, 0.15f, 0.15f, 0.78f);
        background.raycastTarget = true;

        Button button = overlayObject.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = background;
        button.onClick.AddListener(HandleDeleteClicked);

        CreateCrossBar(overlayObject.transform, 45f);
        CreateCrossBar(overlayObject.transform, -45f);

        deleteOverlay = overlayObject;
    }

    private static void CreateCrossBar(Transform parent, float angle)
    {
        GameObject barObject = new GameObject("Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barObject.transform.SetParent(parent, false);

        RectTransform rect = barObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.46f);
        rect.anchorMax = new Vector2(0.85f, 0.54f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);

        Image bar = barObject.GetComponent<Image>();
        bar.color = Color.white;
        bar.raycastTarget = false;
    }

    private void HandleDeleteClicked()
    {
        if (artwork == null)
        {
            return;
        }

        onDelete?.Invoke(artwork);
    }

    private static string ResolveArtworkImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        if (url.StartsWith("http://") || url.StartsWith("https://"))
        {
            return url;
        }

        if (BackendManager.instance == null)
        {
            return url;
        }

        string baseUrl = BackendManager.instance.ApiBaseUrl;
        return url.StartsWith("/") ? baseUrl + url : baseUrl + "/" + url;
    }

    private IEnumerator LoadPreview(string imageUrl)
    {
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return request.SendWebRequest();

        previewLoadCoroutine = null;

        if (request.result != UnityWebRequest.Result.Success || previewImage == null)
        {
            yield break;
        }

        previewImage.texture = DownloadHandlerTexture.GetContent(request);
        pendingPreviewUrl = null;
    }
}
