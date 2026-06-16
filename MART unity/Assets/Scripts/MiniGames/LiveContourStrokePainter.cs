using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Полупрозрачный холст и штрихи прямо на LiveContourArea, поверх картины.
/// </summary>
public sealed class LiveContourStrokePainter
{
    public struct BrushSettings
    {
        public Texture2D StampTexture;
        public Vector2 FallbackStampSize;
        public float BaseStampScale;
        public float ScaleJitter;
        public float RotationJitterDegrees;
        public float OpacityJitter;
        public float StrokeOverlapBlend;
        public bool UseDotStamps;
    }

    private static Sprite whiteSprite;

    private RectTransform paintArea;
    private Image referenceArtwork;
    private Vector2Int textureSize = new Vector2Int(512, 512);
    private float canvasTintAlpha = 0.12f;
    private bool legacyScreenOverlayRemoved;

    private RectTransform paintStack;
    private Image canvasBacking;
    private RawImage paintLayer;
    private Texture2D paintTexture;
    private Color[] clearPixels;

    private Color[] cachedStampPixels;
    private int cachedStampWidth;
    private int cachedStampHeight;
    private Texture2D cachedStampSource;

    public bool IsReady => paintArea != null && paintTexture != null && paintLayer != null && paintStack != null;

    public RectTransform OverlayRoot => paintStack;

    public void Configure(
        RectTransform area,
        Image artwork,
        Vector2Int size,
        float tintAlpha = 0.12f)
    {
        paintArea = area;
        referenceArtwork = artwork;
        textureSize = size;
        canvasTintAlpha = Mathf.Clamp(tintAlpha, 0.04f, 0.4f);

        RemoveLegacyScreenOverlay();
        // Репэрент (AttachToReferenceArtwork) НЕ делаем здесь: Configure вызывается
        // из OnEnable, а Unity запрещает менять иерархию во время активации родителя.
        // Привязку к картине выполняет RefreshLayout из LateUpdate — там это безопасно.
        EnsurePaintStack();
        EnsureTexture();
        EnsureAreaCanvasImage();
    }

    public void SetVisible(bool visible)
    {
        // ВАЖНО: не трогаем paintArea.gameObject — это тот же объект, на котором
        // живёт контроллер LiveContourMiniGame. Если выключить его здесь, он
        // выключит сам себя, и OnEnable больше не сможет вернуть видимость.
        // Прячем/показываем только слой со штрихами.
        if (paintStack != null)
            paintStack.gameObject.SetActive(visible);
    }

    public void RefreshLayout()
    {
        if (paintArea == null || referenceArtwork == null)
            return;

        // Делаем холст дочерним к картине и растягиваем на весь её прямоугольник.
        // Так LiveContourArea всегда точно поверх Image_Playable и едет вместе с
        // ней (включая горизонтальный свайп) — без покадрового пересчёта через
        // камеру, который раньше уводил область в сторону.
        AttachToReferenceArtwork();
        EnsureAreaCanvasImage();
        EnsurePaintStack();
    }

    private void AttachToReferenceArtwork()
    {
        if (paintArea == null || referenceArtwork == null)
            return;

        RectTransform artworkRect = referenceArtwork.rectTransform;
        if (artworkRect == null)
            return;

        if (paintArea.parent != artworkRect)
            paintArea.SetParent(artworkRect, false);

        paintArea.anchorMin = Vector2.zero;
        paintArea.anchorMax = Vector2.one;
        paintArea.offsetMin = Vector2.zero;
        paintArea.offsetMax = Vector2.zero;
        paintArea.pivot = new Vector2(0.5f, 0.5f);
        paintArea.localScale = Vector3.one;
        paintArea.localRotation = Quaternion.identity;
        paintArea.SetAsLastSibling();
    }

    public void Clear()
    {
        EnsureTexture();
        if (paintTexture == null)
            return;

        if (clearPixels == null || clearPixels.Length != paintTexture.width * paintTexture.height)
        {
            clearPixels = new Color[paintTexture.width * paintTexture.height];
            for (int i = 0; i < clearPixels.Length; i++)
                clearPixels[i] = Color.clear;
        }

        paintTexture.SetPixels(clearPixels);
        paintTexture.Apply(false);
        if (paintLayer != null)
            paintLayer.texture = paintTexture;
    }

    public void InvalidateStampCache()
    {
        cachedStampPixels = null;
        cachedStampSource = null;
        cachedStampWidth = 0;
        cachedStampHeight = 0;
    }

    public bool TryScreenToStrokeUv(Vector2 screenPosition, out float u, out float v, out Vector2 areaLocalPoint)
    {
        u = 0f;
        v = 0f;
        areaLocalPoint = default;

        if (paintLayer != null
            && TryScreenToNormalizedUv(paintLayer.rectTransform, screenPosition, out u, out v, out areaLocalPoint))
            return true;

        if (paintArea != null
            && TryScreenToNormalizedUv(paintArea, screenPosition, out u, out v, out areaLocalPoint))
            return true;

        if (referenceArtwork != null
            && TryScreenToArtworkUv(screenPosition, out u, out v, out areaLocalPoint))
            return true;

        return false;
    }

    public void PaintAtUv(float u, float v, Color paintColor, in BrushSettings brush)
    {
        EnsurePaintStack();
        EnsureTexture();
        if (paintTexture == null || paintLayer == null)
            return;

        u = Mathf.Clamp01(u + Random.Range(-0.002f, 0.002f));
        v = Mathf.Clamp01(v + Random.Range(-0.002f, 0.002f));

        int centerX = Mathf.RoundToInt(u * (paintTexture.width - 1));
        int centerY = Mathf.RoundToInt(v * (paintTexture.height - 1));
        float scale = Mathf.Max(1.0f, brush.BaseStampScale * 12f) * Random.Range(1f - brush.ScaleJitter, 1f + brush.ScaleJitter);
        float rotation = Random.Range(-brush.RotationJitterDegrees, brush.RotationJitterDegrees);
        float opacity = Random.Range(1f - brush.OpacityJitter, 1f);
        paintColor.a = Mathf.Clamp01(paintColor.a * opacity);

        // Для точечного режима (пуантилизм) текстуру штампа НЕ используем — она
        // может быть в форме кольца. Рисуем сплошной процедурный кружок.
        bool painted = !brush.UseDotStamps
            && brush.StampTexture != null
            && TryDrawStampTexture(centerX, centerY, scale, rotation, paintColor, brush);

        if (!painted)
            DrawFallbackEllipse(centerX, centerY, scale, paintColor, brush);

        paintTexture.Apply(false);
        paintLayer.texture = paintTexture;
    }

    private void EnsureAreaCanvasImage()
    {
        if (paintArea == null)
            return;

        Image areaImage = paintArea.GetComponent<Image>();
        if (areaImage == null)
            return;

        areaImage.enabled = true;
        areaImage.raycastTarget = false;
        areaImage.sprite = GetWhiteSprite();
        areaImage.color = new Color(1f, 1f, 1f, canvasTintAlpha);
    }

    private void RemoveLegacyScreenOverlay()
    {
        if (legacyScreenOverlayRemoved || paintArea == null)
            return;

        legacyScreenOverlayRemoved = true;

        if (referenceArtwork != null)
            DestroyChildNamed(referenceArtwork.transform, "LiveContourPaintOverlay");

        Transform parent = paintArea.parent;
        if (parent != null)
        {
            Transform legacyOnScreen = parent.Find("LiveContourPaintOverlay");
            if (legacyOnScreen != null && !legacyOnScreen.IsChildOf(paintArea))
                Object.Destroy(legacyOnScreen.gameObject);
        }
    }

    private static void DestroyChildNamed(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            Object.Destroy(child.gameObject);
    }

    private void EnsurePaintStack()
    {
        if (paintArea == null)
            return;

        if (paintStack == null)
        {
            Transform existing = paintArea.Find("LiveContourPaintStack");
            paintStack = existing != null ? existing as RectTransform : null;
        }

        if (paintStack == null)
        {
            GameObject stackObject = new GameObject("LiveContourPaintStack", typeof(RectTransform));
            paintStack = stackObject.GetComponent<RectTransform>();
            paintStack.SetParent(paintArea, false);
            paintStack.anchorMin = Vector2.zero;
            paintStack.anchorMax = Vector2.one;
            paintStack.offsetMin = Vector2.zero;
            paintStack.offsetMax = Vector2.zero;
            paintStack.localScale = Vector3.one;
            paintStack.localRotation = Quaternion.identity;
        }

        if (canvasBacking == null)
        {
            Transform existingBacking = paintStack.Find("CanvasBacking");
            canvasBacking = existingBacking != null ? existingBacking.GetComponent<Image>() : null;
        }

        if (canvasBacking == null)
        {
            GameObject backingObject = new GameObject(
                "CanvasBacking",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform backingRect = backingObject.GetComponent<RectTransform>();
            backingRect.SetParent(paintStack, false);
            backingRect.anchorMin = Vector2.zero;
            backingRect.anchorMax = Vector2.one;
            backingRect.offsetMin = Vector2.zero;
            backingRect.offsetMax = Vector2.zero;
            canvasBacking = backingObject.GetComponent<Image>();
        }

        canvasBacking.sprite = GetWhiteSprite();
        canvasBacking.color = new Color(1f, 1f, 1f, canvasTintAlpha);
        canvasBacking.raycastTarget = false;
        canvasBacking.gameObject.SetActive(true);
        canvasBacking.rectTransform.SetAsFirstSibling();

        if (paintLayer == null)
        {
            Transform existingLayer = paintStack.Find("StrokePaintLayer");
            paintLayer = existingLayer != null ? existingLayer.GetComponent<RawImage>() : null;
        }

        if (paintLayer == null)
        {
            GameObject layerObject = new GameObject(
                "StrokePaintLayer",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            RectTransform layerRect = layerObject.GetComponent<RectTransform>();
            layerRect.SetParent(paintStack, false);
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            paintLayer = layerObject.GetComponent<RawImage>();
        }

        paintLayer.raycastTarget = false;
        paintLayer.color = Color.white;
        paintLayer.gameObject.SetActive(true);
        paintLayer.rectTransform.SetAsLastSibling();
        paintStack.gameObject.SetActive(true);

        if (paintTexture != null)
            paintLayer.texture = paintTexture;
    }

    private bool TryFitRectToReferenceArtwork(RectTransform target)
    {
        if (target == null || referenceArtwork == null)
            return false;

        RectTransform source = referenceArtwork.rectTransform;
        RectTransform parent = target.parent as RectTransform;
        if (parent == null)
            return false;

        Camera cameraForUi = GetCanvasCamera(source);
        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            RectTransformUtility.WorldToScreenPoint(cameraForUi, corners[0]),
            cameraForUi,
            out Vector2 bottomLeft);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            RectTransformUtility.WorldToScreenPoint(cameraForUi, corners[2]),
            cameraForUi,
            out Vector2 topRight);

        Vector2 center = (bottomLeft + topRight) * 0.5f;
        Vector2 size = new Vector2(Mathf.Abs(topRight.x - bottomLeft.x), Mathf.Abs(topRight.y - bottomLeft.y));
        if (size.x < 4f || size.y < 4f)
            return false;

        target.anchorMin = new Vector2(0.5f, 0.5f);
        target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = center;
        target.sizeDelta = size;
        return true;
    }

    private bool TryScreenToNormalizedUv(
        RectTransform rect,
        Vector2 screenPosition,
        out float u,
        out float v,
        out Vector2 localPoint)
    {
        u = 0f;
        v = 0f;
        localPoint = default;

        if (rect == null)
            return false;

        Camera cameraForUi = GetCanvasCamera(rect);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosition, cameraForUi, out localPoint))
            return false;

        Rect rectBounds = rect.rect;
        u = Mathf.Clamp01(Mathf.InverseLerp(rectBounds.xMin, rectBounds.xMax, localPoint.x));
        v = Mathf.Clamp01(Mathf.InverseLerp(rectBounds.yMin, rectBounds.yMax, localPoint.y));
        return true;
    }

    private bool TryScreenToArtworkUv(
        Vector2 screenPosition,
        out float u,
        out float v,
        out Vector2 areaLocalPoint)
    {
        u = 0f;
        v = 0f;
        areaLocalPoint = default;

        if (referenceArtwork == null)
            return false;

        RectTransform artRect = referenceArtwork.rectTransform;
        Camera cameraForUi = GetCanvasCamera(artRect);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(artRect, screenPosition, cameraForUi, out Vector2 artLocal))
            return false;

        if (!TryMapArtworkLocalToUv(artLocal, out u, out v))
            return false;

        if (paintArea != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                paintArea,
                screenPosition,
                GetCanvasCamera(paintArea),
                out areaLocalPoint);
        }
        else
        {
            areaLocalPoint = artLocal;
        }

        return true;
    }

    private bool TryMapArtworkLocalToUv(Vector2 artLocal, out float u, out float v)
    {
        u = 0f;
        v = 0f;

        if (referenceArtwork == null)
            return false;

        Rect rect = referenceArtwork.rectTransform.rect;
        Sprite sprite = referenceArtwork.sprite;

        if (sprite == null)
        {
            u = Mathf.Clamp01(Mathf.InverseLerp(rect.xMin, rect.xMax, artLocal.x));
            v = Mathf.Clamp01(Mathf.InverseLerp(rect.yMin, rect.yMax, artLocal.y));
            return true;
        }

        Rect spriteRect = sprite.rect;
        float spriteAspect = spriteRect.width / Mathf.Max(1f, spriteRect.height);
        float rectAspect = rect.width / Mathf.Max(1f, rect.height);

        float drawWidth;
        float drawHeight;
        float offsetX;
        float offsetY;

        if (spriteAspect > rectAspect)
        {
            drawWidth = rect.width;
            drawHeight = rect.width / spriteAspect;
            offsetX = 0f;
            offsetY = (rect.height - drawHeight) * 0.5f;
        }
        else
        {
            drawHeight = rect.height;
            drawWidth = rect.height * spriteAspect;
            offsetX = (rect.width - drawWidth) * 0.5f;
            offsetY = 0f;
        }

        float localX = artLocal.x - rect.xMin - offsetX;
        float localY = artLocal.y - rect.yMin - offsetY;
        if (localX < 0f || localY < 0f || localX > drawWidth || localY > drawHeight)
            return false;

        u = Mathf.Clamp01(localX / drawWidth);
        v = Mathf.Clamp01(localY / drawHeight);
        return true;
    }

    private void EnsureTexture()
    {
        if (paintTexture != null)
        {
            if (paintLayer != null)
                paintLayer.texture = paintTexture;
            return;
        }

        int width = Mathf.Max(128, textureSize.x);
        int height = Mathf.Max(128, textureSize.y);
        paintTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        paintTexture.wrapMode = TextureWrapMode.Clamp;
        paintTexture.filterMode = FilterMode.Bilinear;
        Clear();

        if (paintLayer != null)
            paintLayer.texture = paintTexture;
    }

    private bool TryDrawStampTexture(
        int centerX,
        int centerY,
        float scale,
        float rotationDegrees,
        Color paintColor,
        in BrushSettings brush)
    {
        if (!TryCacheStampPixels(brush.StampTexture))
            return false;

        // Размер мазка задаём в пикселях холста через FallbackStampSize, а НЕ из
        // разрешения текстуры кисти. Иначе большая streak.png рисует гигантские мазки.
        int width = Mathf.Max(1, Mathf.RoundToInt(brush.FallbackStampSize.x * scale));
        int height = Mathf.Max(1, Mathf.RoundToInt(brush.FallbackStampSize.y * scale));
        int startX = centerX - width / 2;
        int startY = centerY - height / 2;
        float radians = rotationDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        float invWidth = 1f / Mathf.Max(1, width);
        float invHeight = 1f / Mathf.Max(1, height);
        int pixelsWritten = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int targetX = startX + x;
                int targetY = startY + y;

                if (targetX < 0 || targetX >= paintTexture.width || targetY < 0 || targetY >= paintTexture.height)
                    continue;

                float normalizedSourceX = (x * invWidth) - 0.5f;
                float normalizedSourceY = (y * invHeight) - 0.5f;
                float rotatedX = normalizedSourceX * cos - normalizedSourceY * sin;
                float rotatedY = normalizedSourceX * sin + normalizedSourceY * cos;
                float sourceU = rotatedX + 0.5f;
                float sourceV = rotatedY + 0.5f;

                if (sourceU < 0f || sourceU > 1f || sourceV < 0f || sourceV > 1f)
                    continue;

                int sourceX = Mathf.Clamp(Mathf.RoundToInt(sourceU * (cachedStampWidth - 1)), 0, cachedStampWidth - 1);
                int sourceY = Mathf.Clamp(Mathf.RoundToInt(sourceV * (cachedStampHeight - 1)), 0, cachedStampHeight - 1);
                Color source = cachedStampPixels[sourceY * cachedStampWidth + sourceX];
                if (source.a <= 0.01f)
                    continue;

                Color tinted = new Color(
                    paintColor.r,
                    paintColor.g,
                    paintColor.b,
                    Mathf.Clamp01(source.a * paintColor.a * 1.15f));

                Color existing = paintTexture.GetPixel(targetX, targetY);
                paintTexture.SetPixel(targetX, targetY, BlendPixel(existing, tinted, brush.StrokeOverlapBlend));
                pixelsWritten++;
            }
        }

        return pixelsWritten > 0;
    }

    private void DrawFallbackEllipse(int centerX, int centerY, float scale, Color paintColor, in BrushSettings brush)
    {
        float radiusX;
        float radiusY;
        if (brush.UseDotStamps)
        {
            // Точки (пуантилизм) — ровные круги: одинаковый радиус по X и Y.
            float radius = Mathf.Max(6f, brush.FallbackStampSize.x * 0.5f * scale);
            radiusX = radius;
            radiusY = radius;
        }
        else
        {
            radiusX = Mathf.Max(18f, brush.FallbackStampSize.x * 0.5f * scale);
            radiusY = Mathf.Max(12f, brush.FallbackStampSize.y * 0.5f * scale);
        }

        int minX = Mathf.Max(0, Mathf.FloorToInt(centerX - radiusX));
        int maxX = Mathf.Min(paintTexture.width - 1, Mathf.CeilToInt(centerX + radiusX));
        int minY = Mathf.Max(0, Mathf.FloorToInt(centerY - radiusY));
        int maxY = Mathf.Min(paintTexture.height - 1, Mathf.CeilToInt(centerY + radiusY));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = (x - centerX) / radiusX;
                float dy = (y - centerY) / radiusY;
                float distSq = dx * dx + dy * dy;
                if (distSq >= 1f)
                    continue;

                float shape = brush.UseDotStamps
                    ? (distSq < 0.9f ? 1f - distSq * 0.35f : 0f)
                    : Mathf.Pow(1f - distSq, 0.85f);

                if (shape <= 0f)
                    continue;

                Color existing = paintTexture.GetPixel(x, y);
                Color stroke = paintColor;
                stroke.a *= shape;
                paintTexture.SetPixel(x, y, BlendPixel(existing, stroke, brush.StrokeOverlapBlend));
            }
        }
    }

    private static Color BlendPixel(Color existing, Color stroke, float overlapBlend)
    {
        float cover = stroke.a * Mathf.Clamp01(Mathf.Max(overlapBlend, 0.85f));
        if (cover <= 0.001f)
            return existing;

        Color blended = Color.Lerp(existing, stroke, cover);
        blended.a = Mathf.Clamp01(Mathf.Max(existing.a, stroke.a));
        return blended;
    }

    private bool TryCacheStampPixels(Texture2D stampTexture)
    {
        if (stampTexture == null)
            return false;

        if (cachedStampSource == stampTexture && cachedStampPixels != null)
            return true;

        try
        {
            cachedStampPixels = stampTexture.GetPixels();
            cachedStampWidth = stampTexture.width;
            cachedStampHeight = stampTexture.height;
            cachedStampSource = stampTexture;
            return true;
        }
        catch (System.Exception)
        {
            // Не-readable текстура кисти бросает ArgumentException, а не UnityException.
            // Ловим любой тип и откатываемся на запасную кисть (эллипс).
            cachedStampPixels = null;
            cachedStampSource = null;
            return false;
        }
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
            return whiteSprite;

        Texture2D texture = Texture2D.whiteTexture;
        whiteSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));
        return whiteSprite;
    }

    private static Camera GetCanvasCamera(RectTransform rect)
    {
        if (rect == null)
            return null;

        Canvas parentCanvas = rect.GetComponentInParent<Canvas>();
        if (parentCanvas == null)
            return null;

        return parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main;
    }
}
