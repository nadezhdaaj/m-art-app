using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Оборачивает Image аватара в UI Mask с круглой формой.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class CircularAvatarMask : MonoBehaviour
{
    private const int CircleSpriteResolution = 256;

    private static Sprite sharedCircleMaskSprite;

    private bool isApplied;

    private void Awake()
    {
        Apply();
    }

    public void Apply()
    {
        if (isApplied)
        {
            return;
        }

        Image avatarImage = GetComponent<Image>();
        if (avatarImage == null)
        {
            return;
        }

        if (transform.parent != null && transform.parent.name == "AvatarCircleMask")
        {
            avatarImage.preserveAspect = true;
            isApplied = true;
            return;
        }

        RectTransform avatarRect = (RectTransform)transform;
        Transform originalParent = avatarRect.parent;
        int siblingIndex = avatarRect.GetSiblingIndex();

        GameObject maskRoot = new GameObject(
            "AvatarCircleMask",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Mask));

        RectTransform maskRect = maskRoot.GetComponent<RectTransform>();
        maskRect.SetParent(originalParent, false);
        maskRect.SetSiblingIndex(siblingIndex);
        maskRect.anchorMin = avatarRect.anchorMin;
        maskRect.anchorMax = avatarRect.anchorMax;
        maskRect.pivot = avatarRect.pivot;
        maskRect.anchoredPosition = avatarRect.anchoredPosition;
        float side = Mathf.Min(avatarRect.sizeDelta.x, avatarRect.sizeDelta.y);
        maskRect.sizeDelta = new Vector2(side, side);
        maskRect.localRotation = avatarRect.localRotation;
        maskRect.localScale = avatarRect.localScale;

        avatarRect.SetParent(maskRect, false);
        avatarRect.anchorMin = Vector2.zero;
        avatarRect.anchorMax = Vector2.one;
        avatarRect.offsetMin = Vector2.zero;
        avatarRect.offsetMax = Vector2.zero;
        avatarRect.anchoredPosition = Vector2.zero;
        avatarRect.localRotation = Quaternion.identity;
        avatarRect.localScale = Vector3.one;

        Image maskImage = maskRoot.GetComponent<Image>();
        maskImage.sprite = GetCircleMaskSprite();
        maskImage.type = Image.Type.Simple;
        maskImage.raycastTarget = false;
        maskImage.color = Color.white;

        Mask mask = maskRoot.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        avatarImage.preserveAspect = true;
        isApplied = true;
    }

    public static Sprite CreateAvatarSprite(Texture2D source)
    {
        if (source == null)
        {
            return null;
        }

        Texture2D square = CropToSquareTexture(source);
        if (square == null)
        {
            return null;
        }

        return Sprite.Create(
            square,
            new Rect(0, 0, square.width, square.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    public static Texture2D CropToSquareTexture(Texture2D source)
    {
        if (source == null)
        {
            return null;
        }

        int edge = Mathf.Min(source.width, source.height);
        if (edge <= 0)
        {
            return null;
        }

        int startX = (source.width - edge) / 2;
        int startY = (source.height - edge) / 2;
        Color[] pixels = source.GetPixels(startX, startY, edge, edge);

        Texture2D square = new Texture2D(edge, edge, TextureFormat.RGBA32, false);
        square.SetPixels(pixels);
        square.Apply(false, false);
        return square;
    }

    private static Sprite GetCircleMaskSprite()
    {
        if (sharedCircleMaskSprite != null)
        {
            return sharedCircleMaskSprite;
        }

        int size = CircleSpriteResolution;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * 0.5f;
        Vector2 center = new Vector2(radius, radius);

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                pixels[y * size + x] = distance <= radius ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        sharedCircleMaskSprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f);
        sharedCircleMaskSprite.name = "AvatarCircleMaskGenerated";

        return sharedCircleMaskSprite;
    }
}
