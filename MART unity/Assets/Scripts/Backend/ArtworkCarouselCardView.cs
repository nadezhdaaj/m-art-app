using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Compact artwork card for the profile horizontal carousel.
/// </summary>
public class ArtworkCarouselCardView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text characteristic1Text;
    [SerializeField] private TMP_Text characteristic2Text;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Button openButton;

    private ArtworkDto artwork;
    private Action<ArtworkDto> onSelected;
    private string pendingPreviewUrl;
    private Coroutine previewLoadCoroutine;

    private void Awake()
    {
        ResolveReferences();
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

    private void ResolveReferences()
    {
        if (titleText == null)
        {
            titleText = transform.Find("Title")?.GetComponent<TMP_Text>();
        }

        if (characteristic1Text == null)
        {
            characteristic1Text = transform.Find("Characteristic1")?.GetComponent<TMP_Text>();
        }

        if (characteristic2Text == null)
        {
            characteristic2Text = transform.Find("Characteristic2")?.GetComponent<TMP_Text>();
        }

        if (previewImage == null)
        {
            previewImage = transform.Find("Preview")?.GetComponent<RawImage>();
        }

        if (openButton == null)
        {
            openButton = GetComponent<Button>();
        }
    }

    public void Bind(ArtworkDto value, Action<ArtworkDto> onSelectedCallback)
    {
        artwork = value;
        onSelected = onSelectedCallback;
        ResolveReferences();

        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(value?.title) ? "Название работы" : value.title;
        }

        if (characteristic1Text != null)
        {
            characteristic1Text.text = BuildCharacteristic1(value);
        }

        if (characteristic2Text != null)
        {
            characteristic2Text.text = BuildCharacteristic2(value);
        }

        if (openButton != null)
        {
            openButton.onClick.RemoveListener(HandleSelected);
            openButton.onClick.AddListener(HandleSelected);
        }

        if (previewImage != null)
        {
            previewImage.raycastTarget = false;
            previewImage.texture = null;
            previewImage.color = new Color(0.55f, 0.55f, 0.58f, 1f);
        }

        pendingPreviewUrl = null;
        if (previewImage != null && !string.IsNullOrWhiteSpace(value?.thumbnailUrl ?? value?.imageUrl))
        {
            pendingPreviewUrl = ResolveArtworkImageUrl(value.thumbnailUrl ?? value.imageUrl);
        }

        TryStartPreviewLoad();
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

    private IEnumerator LoadPreview(string url)
    {
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (previewImage == null)
        {
            yield break;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(request);
        previewImage.texture = texture;
        previewImage.color = Color.white;
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

    private static string BuildCharacteristic1(ArtworkDto value)
    {
        if (!string.IsNullOrWhiteSpace(value?.description))
        {
            return Truncate(value.description, 42);
        }

        if (!string.IsNullOrWhiteSpace(value?.kind))
        {
            return value.kind;
        }

        return "Характеристика 1";
    }

    private static string BuildCharacteristic2(ArtworkDto value)
    {
        if (!string.IsNullOrWhiteSpace(value?.status))
        {
            return value.status;
        }

        if (!string.IsNullOrWhiteSpace(value?.source))
        {
            return value.source;
        }

        return "Характеристика 2";
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength - 1) + "…";
    }

    private void HandleSelected()
    {
        onSelected?.Invoke(artwork);
    }
}
