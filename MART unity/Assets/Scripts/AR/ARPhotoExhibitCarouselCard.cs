using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARPhotoExhibitCarouselCard : MonoBehaviour
{
    [SerializeField] private Image previewImage;
    [SerializeField] private Image selectionOutline;
    [SerializeField] private Button button;

    private string exhibitId;
    private Action<string> onSelected;

    public string ExhibitId => exhibitId;

    private void Awake()
    {
        EnsureReferences();
    }

    public void Bind(string id, Sprite preview, Color fallbackColor, bool selected, Action<string> onSelectedCallback)
    {
        EnsureReferences();
        exhibitId = id;
        onSelected = onSelectedCallback;

        if (previewImage != null)
        {
            if (preview != null)
            {
                previewImage.sprite = preview;
                previewImage.color = Color.white;
            }
            else
            {
                previewImage.sprite = null;
                previewImage.color = fallbackColor;
            }

            previewImage.preserveAspect = true;
            previewImage.raycastTarget = false;
        }

        if (selectionOutline != null)
        {
            selectionOutline.enabled = selected;
        }

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectionOutline != null)
        {
            selectionOutline.enabled = selected;
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        onSelected?.Invoke(exhibitId);
    }

    private void EnsureReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (previewImage == null)
        {
            Transform preview = transform.Find("Preview");
            if (preview != null)
            {
                previewImage = preview.GetComponent<Image>();
            }
        }

        if (selectionOutline == null)
        {
            Transform selection = transform.Find("Selection");
            if (selection != null)
            {
                selectionOutline = selection.GetComponent<Image>();
            }
        }
    }
}
