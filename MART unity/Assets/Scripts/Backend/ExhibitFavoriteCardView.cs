using System;
using UnityEngine;
using UnityEngine.UI;

public class ExhibitFavoriteCardView : MonoBehaviour
{
    [SerializeField] private Image coverImage;
    [SerializeField] private Button openButton;

    private string exhibitId;
    private Action<string> onSelected;

    private void Awake()
    {
        if (coverImage == null)
        {
            coverImage = GetComponent<Image>();
        }

        if (openButton == null)
        {
            openButton = GetComponent<Button>();
        }
    }

    public void Bind(string value, Sprite coverSprite, Action<string> onSelectedCallback)
    {
        exhibitId = value;
        onSelected = onSelectedCallback;

        if (coverImage != null && coverSprite != null)
        {
            coverImage.sprite = coverSprite;
            coverImage.preserveAspect = true;
            coverImage.raycastTarget = false;
        }

        if (openButton != null)
        {
            openButton.onClick.RemoveListener(HandleSelected);
            openButton.onClick.AddListener(HandleSelected);
        }
    }

    private void OnDisable()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(HandleSelected);
        }
    }

    private void HandleSelected()
    {
        onSelected?.Invoke(exhibitId);
    }
}
