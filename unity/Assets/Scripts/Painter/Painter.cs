using System;
using UnityEngine;
using UnityEngine.UI;

public class Painter : MonoBehaviour
{
    [Serializable]
    public struct StrokeSummary
    {
        public float durationSeconds;
        public float distancePixels;
        public float averageSpeed;
        public int sampledPoints;
    }

    private struct BrushSettings
    {
        public float sizeMultiplier;
        public float opacity;
        public float spacing;
        public float grain;
    }

    public enum BrushType
    {
        Regular = 0,
        Chalk = 1,
        Marker = 2,
        Pencil = 3
    }

    [Header("Canvas")]
    public RawImage rawImage;
    public int textureWidth = 1024;
    public int textureHeight = 1024;

    [Header("Brush")]
    public Color brushColor = Color.black;
    public int brushSize = 24;
    public BrushType brushType = BrushType.Regular;
    public bool canDraw = true;
    public bool isEraser = false;

    [Header("Brush Textures")]
    public Texture2D brushStamp;
    public Texture2D chalkStamp;
    public Texture2D markerStamp;
    public Texture2D pencilStamp;

    private Texture2D canvasTexture;
    private Texture2D currentStamp;
    private Texture2D fallbackStamp;
    private bool isDrawing;
    private Vector2 lastDrawPosition;
    private float strokeStartTime;
    private float strokeDistance;
    private int strokeSampleCount;

    public event Action<StrokeSummary> OnStrokeFinished;

    private void Awake()
    {
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();

        CreateFallbackStamp();
        CreateCanvasTexture();
        SetBrushType(brushType);
    }

    private void OnEnable()
    {
        EnsureCanvasAssigned();
    }

    private void Update()
    {
        if (!canDraw || rawImage == null || canvasTexture == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 point = ScreenToTexturePoint(Input.mousePosition);
            if (point.x < 0f)
                return;

            BeginStroke(point);
        }

        if (Input.GetMouseButton(0) && isDrawing)
        {
            Vector2 point = ScreenToTexturePoint(Input.mousePosition);
            if (point.x < 0f)
                return;

            ContinueStroke(point);
        }

        if (Input.GetMouseButtonUp(0) && isDrawing)
            EndStroke();
    }

    public void ClearCanvas()
    {
        if (canvasTexture == null)
            return;

        Color[] pixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        canvasTexture.SetPixels(pixels);
        canvasTexture.Apply();
        EnsureCanvasAssigned();
    }

    public void SetBrushType(BrushType type)
    {
        brushType = type;
        currentStamp = GetStampForType(type);
        isEraser = false;
    }

    public void SetBrush()
    {
        SetBrushType(BrushType.Regular);
    }

    public void SetEraser()
    {
        isEraser = true;
    }

    public void EnableDrawing()
    {
        canDraw = true;
    }

    public void DisableDrawing()
    {
        canDraw = false;
    }

    public void SetColor(Color newColor)
    {
        brushColor = newColor;
        isEraser = false;
    }

    private void CreateCanvasTexture()
    {
        if (rawImage == null)
        {
            Debug.LogError("Painter: RawImage is not assigned.");
            return;
        }

        canvasTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        canvasTexture.name = "PainterCanvasTexture";
        canvasTexture.filterMode = FilterMode.Bilinear;
        canvasTexture.wrapMode = TextureWrapMode.Clamp;

        ClearCanvas();
    }

    private void EnsureCanvasAssigned()
    {
        if (rawImage != null && canvasTexture != null && rawImage.texture != canvasTexture)
            rawImage.texture = canvasTexture;
    }

    private Vector2 ScreenToTexturePoint(Vector2 screenPosition)
    {
        if (rawImage == null)
            return new Vector2(-1f, -1f);

        RectTransform rectTransform = rawImage.rectTransform;
        Camera cameraForUi = null;

        Canvas parentCanvas = rawImage.canvas;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cameraForUi = parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main;

        Vector2 localPoint;
        bool inside = RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, cameraForUi, out localPoint);
        if (!inside)
            return new Vector2(-1f, -1f);

        float normalizedX = Mathf.InverseLerp(rectTransform.rect.xMin, rectTransform.rect.xMax, localPoint.x);
        float normalizedY = Mathf.InverseLerp(rectTransform.rect.yMin, rectTransform.rect.yMax, localPoint.y);

        if (normalizedX < 0f || normalizedX > 1f || normalizedY < 0f || normalizedY > 1f)
            return new Vector2(-1f, -1f);

        return new Vector2(normalizedX * (textureWidth - 1), normalizedY * (textureHeight - 1));
    }

    private void BeginStroke(Vector2 point)
    {
        isDrawing = true;
        lastDrawPosition = point;
        strokeStartTime = Time.time;
        strokeDistance = 0f;
        strokeSampleCount = 1;
        DrawLine(point, point);
    }

    private void ContinueStroke(Vector2 point)
    {
        strokeDistance += Vector2.Distance(lastDrawPosition, point);
        strokeSampleCount++;
        DrawLine(lastDrawPosition, point);
        lastDrawPosition = point;
    }

    private void EndStroke()
    {
        isDrawing = false;

        float duration = Mathf.Max(0.01f, Time.time - strokeStartTime);
        StrokeSummary summary = new StrokeSummary
        {
            durationSeconds = duration,
            distancePixels = strokeDistance,
            averageSpeed = strokeDistance / duration,
            sampledPoints = strokeSampleCount
        };

        OnStrokeFinished?.Invoke(summary);
    }

    private void DrawLine(Vector2 from, Vector2 to)
    {
        if (canvasTexture == null)
            return;

        BrushSettings settings = GetBrushSettings();
        float distance = Vector2.Distance(from, to);
        float stepSize = Mathf.Max(1f, brushSize * settings.spacing);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / stepSize));

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 point = Vector2.Lerp(from, to, t);
            DrawStamp(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));
        }

        canvasTexture.Apply();
    }

    private void DrawStamp(int centerX, int centerY)
    {
        Texture2D stamp = currentStamp != null ? currentStamp : fallbackStamp;
        if (stamp == null || canvasTexture == null)
            return;

        BrushSettings settings = GetBrushSettings();
        int diameter = Mathf.Max(1, Mathf.RoundToInt(brushSize * settings.sizeMultiplier));

        for (int y = 0; y < diameter; y++)
        {
            for (int x = 0; x < diameter; x++)
            {
                float u = diameter == 1 ? 0.5f : x / (float)(diameter - 1);
                float v = diameter == 1 ? 0.5f : y / (float)(diameter - 1);
                Color mask = stamp.GetPixelBilinear(u, v);

                if (mask.a <= 0.01f)
                    continue;

                int px = centerX + x - diameter / 2;
                int py = centerY + y - diameter / 2;

                if (px < 0 || px >= textureWidth || py < 0 || py >= textureHeight)
                    continue;

                if (isEraser)
                {
                    canvasTexture.SetPixel(px, py, Color.white);
                    continue;
                }

                Color background = canvasTexture.GetPixel(px, py);
                float alpha = mask.a * settings.opacity;

                if (settings.grain > 0f)
                {
                    float noise = UnityEngine.Random.value;
                    alpha *= Mathf.Lerp(1f - settings.grain, 1f, noise);
                }

                Color blended = Color.Lerp(background, brushColor, alpha);
                canvasTexture.SetPixel(px, py, blended);
            }
        }
    }

    private BrushSettings GetBrushSettings()
    {
        switch (brushType)
        {
            case BrushType.Chalk:
                return new BrushSettings
                {
                    sizeMultiplier = 1.35f,
                    opacity = 0.38f,
                    spacing = 1.25f,
                    grain = 0.65f
                };

            case BrushType.Marker:
                return new BrushSettings
                {
                    sizeMultiplier = 1.8f,
                    opacity = 0.22f,
                    spacing = 0.35f,
                    grain = 0f
                };

            case BrushType.Pencil:
                return new BrushSettings
                {
                    sizeMultiplier = 0.4f,
                    opacity = 0.9f,
                    spacing = 0.18f,
                    grain = 0.25f
                };

            default:
                return new BrushSettings
                {
                    sizeMultiplier = 1.05f,
                    opacity = 0.8f,
                    spacing = 0.55f,
                    grain = 0.03f
                };
        }
    }


    private Texture2D GetStampForType(BrushType type)
    {
        switch (type)
        {
            case BrushType.Chalk:
                return GetReadableTexture(chalkStamp);
            case BrushType.Marker:
                return GetReadableTexture(markerStamp);
            case BrushType.Pencil:
                return GetReadableTexture(pencilStamp);
            default:
                return GetReadableTexture(brushStamp);
        }
    }

    private Texture2D GetReadableTexture(Texture2D source)
    {
        if (source == null)
            return fallbackStamp;

        try
        {
            source.GetPixelBilinear(0.5f, 0.5f);
            return source;
        }
        catch
        {
            return CopyTexture(source);
        }
    }

    private static Texture2D CopyTexture(Texture2D source)
    {
        RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, temporary);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = temporary;

        Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        copy.Apply();
        copy.filterMode = FilterMode.Bilinear;
        copy.wrapMode = TextureWrapMode.Clamp;

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temporary);
        return copy;
    }

    private void CreateFallbackStamp()
    {
        if (fallbackStamp != null)
            return;

        fallbackStamp = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        fallbackStamp.filterMode = FilterMode.Bilinear;
        fallbackStamp.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(31.5f, 31.5f);
        float radius = 31f;

        for (int y = 0; y < fallbackStamp.height; y++)
        {
            for (int x = 0; x < fallbackStamp.width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - distance / radius);
                fallbackStamp.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        fallbackStamp.Apply();
    }
}
