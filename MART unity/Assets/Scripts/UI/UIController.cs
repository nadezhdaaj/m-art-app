using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] private GameObject infoPanel;

    private const string ViewedBadgeResourcePath = "Icons/ar-viewed-badge";

    private ARImageSpawner imageSpawner;
    private Image viewedBadgeImage;

    private void Awake()
    {
        EnsureViewedBadge();
    }

    private void Start()
    {
        ARSceneModeApplier.TryApply(this);
    }

    public void ApplyEntryMode(bool isScanning)
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(isScanning);
        }
    }

    private void OnEnable()
    {
        BindToImageSpawner();
        RefreshViewedBadge();
    }

    private void OnDisable()
    {
        UnbindFromImageSpawner();
    }


    public void ToggleInfo()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(!infoPanel.activeSelf);
            RefreshViewedBadge();
        }
    }

    private void BindToImageSpawner()
    {
        if (imageSpawner == null)
        {
            imageSpawner = FindObjectOfType<ARImageSpawner>();
        }

        if (imageSpawner == null)
        {
            return;
        }

        imageSpawner.CurrentExhibitChanged -= HandleCurrentExhibitChanged;
        imageSpawner.ExhibitViewedStateChanged -= HandleViewedStateChanged;
        imageSpawner.CurrentExhibitChanged += HandleCurrentExhibitChanged;
        imageSpawner.ExhibitViewedStateChanged += HandleViewedStateChanged;
    }

    private void UnbindFromImageSpawner()
    {
        if (imageSpawner == null)
        {
            return;
        }

        imageSpawner.CurrentExhibitChanged -= HandleCurrentExhibitChanged;
        imageSpawner.ExhibitViewedStateChanged -= HandleViewedStateChanged;
    }

    private void HandleCurrentExhibitChanged(string exhibitId)
    {
        RefreshViewedBadge();
    }

    private void HandleViewedStateChanged(string exhibitId, bool isViewed)
    {
        RefreshViewedBadge();
    }

    private void EnsureViewedBadge()
    {
        if (infoPanel == null || viewedBadgeImage != null)
        {
            return;
        }

        Transform existingBadge = infoPanel.transform.Find("ViewedBadge");
        if (existingBadge != null)
        {
            viewedBadgeImage = existingBadge.GetComponent<Image>();
            return;
        }

        GameObject badgeObject = new GameObject("ViewedBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badgeObject.transform.SetParent(infoPanel.transform, false);

        RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1f, 1f);
        badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(1f, 1f);
        badgeRect.sizeDelta = new Vector2(54f, 54f);
        badgeRect.anchoredPosition = new Vector2(-18f, -18f);

        viewedBadgeImage = badgeObject.GetComponent<Image>();
        viewedBadgeImage.raycastTarget = false;
        viewedBadgeImage.preserveAspect = true;
        viewedBadgeImage.sprite = Resources.Load<Sprite>(ViewedBadgeResourcePath);
        viewedBadgeImage.enabled = viewedBadgeImage.sprite != null;
        badgeObject.SetActive(false);
    }

    private void RefreshViewedBadge()
    {
        EnsureViewedBadge();
        BindToImageSpawner();

        if (viewedBadgeImage == null)
        {
            return;
        }

        bool shouldShow = infoPanel != null &&
            infoPanel.activeSelf &&
            imageSpawner != null &&
            !string.IsNullOrWhiteSpace(imageSpawner.CurrentExhibitId) &&
            imageSpawner.IsExhibitViewed(imageSpawner.CurrentExhibitId);

        viewedBadgeImage.gameObject.SetActive(shouldShow);
    }
}
