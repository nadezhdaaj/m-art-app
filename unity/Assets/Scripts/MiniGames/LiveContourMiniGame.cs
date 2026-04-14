using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LiveContourMiniGame : MonoBehaviour
{
    [Serializable]
    private class StageDefinition
    {
        public string title;
        [TextArea(2, 4)] public string hint;
        [Min(1)] public int requiredStrokeCount = 8;
        public int[] allowedPaletteIndices;
    }

    [Serializable]
    private class LightToken
    {
        public RectTransform rectTransform;
        public Vector2 localPoint;
        public float remainingLifetime;
    }

    private static Sprite debugSprite;
    private static bool loggedUnreadableTextureWarning;

    [Header("Input")]
    [SerializeField] private RectTransform inputArea;
    [SerializeField] private Canvas targetCanvas;

    [Header("Visual")]
    [SerializeField] private Texture2D stampTexture;
    [SerializeField] private Color fallbackStampColor = new Color(0.08f, 0.08f, 0.08f, 0.88f);
    [SerializeField] private Vector2Int canvasSize = new Vector2Int(512, 512);
    [SerializeField] private Vector2 fallbackStampSize = new Vector2(72f, 28f);
    [SerializeField] private float dragSpacingPixels = 18f;
    [SerializeField] private float baseStampScale = 0.08f;
    [SerializeField] private float scaleJitter = 0.08f;
    [SerializeField] private float rotationJitterDegrees = 10f;
    [SerializeField] private float opacityJitter = 0.08f;
    [SerializeField] private float positionJitterPixels = 2f;
    [SerializeField] private bool showDebugMarkers;

    [Header("Palette")]
    [SerializeField] private Color[] paletteColors;
    [SerializeField] private Image[] paletteSwatches;
    [SerializeField] private int defaultPaletteIndex;
    [SerializeField] private float inactivePaletteAlpha = 0.3f;
    [SerializeField] private float activePaletteAlpha = 1f;
    [SerializeField] private float selectedPaletteScale = 1.08f;
    [SerializeField] private float unselectedPaletteScale = 1f;

    [Header("Stages")]
    [SerializeField] private StageDefinition[] stages;

    [Header("HUD")]
    [SerializeField] private TMP_Text stageTitleText;
    [SerializeField] private TMP_Text stageHintText;
    [SerializeField] private TMP_Text strokeProgressText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text bonusScoreText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button nextStageButton;

    [Header("Timing")]
    [SerializeField] private float stageDurationSeconds = 20f;
    [SerializeField] private float idleHintDelaySeconds = 4f;
    [SerializeField] private string idleHintMessage = "Если не знаешь, с чего начать, сделай несколько штрихов подходящим цветом.";

    [Header("Light Bonus")]
    [SerializeField] private int totalLightsRequired = 5;
    [SerializeField] private float lightSpawnIntervalMin = 3f;
    [SerializeField] private float lightSpawnIntervalMax = 6f;
    [SerializeField] private float lightLifetimeSeconds = 3.5f;
    [SerializeField] private float lightCatchRadiusPixels = 80f;
    [SerializeField] private int lightBonusPoints = 3;
    [SerializeField] private Vector2 lightVisualSize = new Vector2(88f, 88f);
    [SerializeField] private Color lightVisualColor = new Color(1f, 0.88f, 0.35f, 0.95f);

    [Header("Light Ray Bonus (Golden Ray)")]
    [SerializeField] private float lightRaySize = 120f;
    [SerializeField] private Color lightRayColor = new Color(1f, 0.95f, 0.5f, 0.6f);
    [SerializeField] private float colorSaturationBonus = 0.4f;
    [SerializeField] private float colorBrightnessBonus = 0.3f;

    private Camera uiCamera;
    private RawImage paintLayer;
    private Texture2D paintTexture;
    private Color[] clearPixels;
    private Vector2 lastStampLocalPosition;
    private bool wasPointerHeld;
    private RectTransform canvasRect;
    private RectTransform debugOverlayRoot;
    private bool paintDirty;
    private Color[] cachedStampPixels;
    private int cachedStampWidth;
    private int cachedStampHeight;
    private Texture2D cachedStampSource;
    private int currentPaletteIndex;
    private int currentStageIndex;
    private int currentStageStrokeCount;
    private bool currentStrokeAlreadyCounted;
    private float stageTimeRemaining;
    private float idleTime;
    private bool idleHintShown;
    private bool stageExpired;
    private float nextLightSpawnDelay;
    private int bonusScore;
    private int lightsCollectedThisStage;
    private bool lightRayActive;
    private Vector2 lightRayPosition;
    private RectTransform lightRayDisplay;
    private readonly System.Collections.Generic.List<LightToken> activeLights = new System.Collections.Generic.List<LightToken>();

    private void Awake()
    {
        if (inputArea == null)
            inputArea = transform as RectTransform;

        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        EnsureDefaultPalette();
        EnsureDefaultStages();
        ResolveCamera();
        EnsureDebugOverlayRoot();
        EnsurePaintLayer();
        currentPaletteIndex = Mathf.Clamp(defaultPaletteIndex, 0, Mathf.Max(0, paletteColors.Length - 1));
        bonusScore = 0;
        lightsCollectedThisStage = 0;
        lightRayActive = false;
        UpdateBonusScoreText();
        ClearCanvas();
        ApplyStage(0, true);
        UpdatePaletteVisuals();
        EnsureLightRayDisplay();
    }

    private void Update()
    {
        UpdateTimers();
        UpdateLightTokens();
        UpdateLightRayDisplay();

        bool pointerHeld = Input.GetMouseButton(0);

        if (!pointerHeld)
        {
            wasPointerHeld = false;
            currentStrokeAlreadyCounted = false;
            FlushPaint();
            return;
        }

        if (!TryGetLocalPoint(Input.mousePosition, out Vector2 localPoint))
        {
            FlushPaint();
            return;
        }

        if (!wasPointerHeld)
        {
            BeginStroke(localPoint);
            return;
        }

        ContinueStroke(localPoint);
    }

    public void SelectPaletteColor(int paletteIndex)
    {
        if (paletteColors == null || paletteColors.Length == 0)
            return;

        currentPaletteIndex = Mathf.Clamp(paletteIndex, 0, paletteColors.Length - 1);
        UpdatePaletteVisuals();
        UpdateFeedbackForCurrentColor();
    }

    public void NextStage()
    {
        if (stages == null || stages.Length == 0)
            return;

        int nextIndex = Mathf.Min(currentStageIndex + 1, stages.Length - 1);
        ApplyStage(nextIndex, false);
    }

    public void PreviousStage()
    {
        if (stages == null || stages.Length == 0)
            return;

        int previousIndex = Mathf.Max(currentStageIndex - 1, 0);
        ApplyStage(previousIndex, false);
    }

    public void RestartMiniGame()
    {
        ClearCanvas();
        bonusScore = 0;
        lightsCollectedThisStage = 0;
        lightRayActive = false;
        UpdateBonusScoreText();
        ApplyStage(0, true);
    }

    public void ClearCanvas()
    {
        EnsurePaintLayer();
        if (paintTexture == null)
            return;

        currentStrokeAlreadyCounted = false;
        ClearLightTokens();

        if (clearPixels == null || clearPixels.Length != paintTexture.width * paintTexture.height)
            InitializeClearPixels();

        paintTexture.SetPixels(clearPixels);
        paintTexture.Apply(false);
        paintDirty = false;
        ClearDebugMarkers();
    }

    private void BeginStroke(Vector2 localPoint)
    {
        idleTime = 0f;
        idleHintShown = false;
        bool caughtLight = TryCatchLight(localPoint);
        if (!caughtLight)
        {
            PlaceStamp(localPoint);
            CreateDebugMarker(localPoint);
        }
        lastStampLocalPosition = localPoint;
        wasPointerHeld = true;
        TryRegisterStroke();
        FlushPaint();
    }

    private void ContinueStroke(Vector2 localPoint)
    {
        float distance = Vector2.Distance(lastStampLocalPosition, localPoint);
        if (distance < Mathf.Max(1f, dragSpacingPixels))
        {
            FlushPaint();
            return;
        }

        Vector2 direction = (localPoint - lastStampLocalPosition).normalized;
        float travelled = dragSpacingPixels;

        while (travelled <= distance)
        {
            Vector2 point = lastStampLocalPosition + direction * travelled;
            bool caughtLight = TryCatchLight(point);
            if (!caughtLight)
            {
                PlaceStamp(point);
                CreateDebugMarker(point);
            }
            travelled += dragSpacingPixels;
        }

        lastStampLocalPosition = localPoint;
        FlushPaint();
    }

    private void TryRegisterStroke()
    {
        if (currentStrokeAlreadyCounted)
            return;

        currentStrokeAlreadyCounted = true;

        if (IsCurrentColorAllowed())
        {
            currentStageStrokeCount++;
            UpdateProgressText();

            if (currentStageStrokeCount >= GetCurrentStage().requiredStrokeCount)
            {
                SetFeedback("Этап готов. Можно нажать \"Дальше\".");
                SetNextStageButtonState(true);
                stageExpired = true;
            }
            else
            {
                SetFeedback("Хорошо. Продолжай в этом цвете.");
            }
        }
        else
        {
            SetFeedback("Для этого этапа лучше выбрать другой цвет из палитры.");
        }
    }

    private void ApplyStage(int stageIndex, bool force)
    {
        if (stages == null || stages.Length == 0)
        {
            UpdateProgressText();
            return;
        }

        int clampedIndex = Mathf.Clamp(stageIndex, 0, stages.Length - 1);
        if (!force && clampedIndex == currentStageIndex)
            return;

        currentStageIndex = clampedIndex;
        currentStageStrokeCount = 0;
        currentStrokeAlreadyCounted = false;
        stageTimeRemaining = Mathf.Max(1f, stageDurationSeconds);
        idleTime = 0f;
        idleHintShown = false;
        stageExpired = false;
        lightsCollectedThisStage = 0;
        lightRayActive = false;
        nextLightSpawnDelay = UnityEngine.Random.Range(lightSpawnIntervalMin, lightSpawnIntervalMax);
        ClearLightTokens();
        UpdateStageTexts();
        UpdateProgressText();
        UpdateTimerText();
        UpdatePaletteVisuals();
        SetNextStageButtonState(false);
        UpdateFeedbackForCurrentColor();
    }

    private void UpdateStageTexts()
    {
        StageDefinition stage = GetCurrentStage();

        if (stageTitleText != null)
            stageTitleText.text = string.IsNullOrWhiteSpace(stage.title)
                ? $"Этап {currentStageIndex + 1}"
                : stage.title;

        if (stageHintText != null)
            stageHintText.text = stage.hint ?? string.Empty;
    }

    private void UpdateProgressText()
    {
        if (strokeProgressText == null)
            return;

        StageDefinition stage = GetCurrentStage();
        if (stage == null)
        {
            strokeProgressText.text = string.Empty;
            return;
        }

        int clampedProgress = Mathf.Min(currentStageStrokeCount, stage.requiredStrokeCount);
        strokeProgressText.text = $"Штрихи: {clampedProgress} / {stage.requiredStrokeCount}";
    }

    private void UpdateFeedbackForCurrentColor()
    {
        if (GetCurrentStage() == null)
            return;

        if (IsCurrentColorAllowed())
            SetFeedback("Цвет подходит для текущего этапа.");
        else
            SetFeedback("Выбери один из подсвеченных цветов.");
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
    }

    private void UpdateBonusScoreText()
    {
        if (bonusScoreText != null)
            bonusScoreText.text = $"Свет: {bonusScore}";
    }

    private void UpdateTimers()
    {
        if (GetCurrentStage() == null || stageExpired)
            return;

        stageTimeRemaining = Mathf.Max(0f, stageTimeRemaining - Time.deltaTime);
        idleTime += Time.deltaTime;
        UpdateTimerText();

        if (!idleHintShown && idleTime >= Mathf.Max(0.5f, idleHintDelaySeconds))
        {
            idleHintShown = true;
            SetFeedback(idleHintMessage);
        }

        if (stageTimeRemaining <= 0f)
        {
            stageExpired = true;
            SetFeedback("Время этапа закончилось. Можно перейти дальше или попробовать еще раз.");
            SetNextStageButtonState(true);
        }
    }

    private void UpdateLightTokens()
    {
        if (inputArea == null || stageExpired)
            return;

        nextLightSpawnDelay -= Time.deltaTime;
        if (nextLightSpawnDelay <= 0f)
        {
            SpawnLightToken();
            nextLightSpawnDelay = UnityEngine.Random.Range(lightSpawnIntervalMin, lightSpawnIntervalMax);
        }

        for (int i = activeLights.Count - 1; i >= 0; i--)
        {
            LightToken token = activeLights[i];
            token.remainingLifetime -= Time.deltaTime;

            if (token.rectTransform != null)
            {
                float pulse = 0.92f + Mathf.Sin(Time.time * 8f + i) * 0.08f;
                token.rectTransform.localScale = Vector3.one * pulse;
            }

            if (token.remainingLifetime <= 0f)
                RemoveLightTokenAt(i);
        }
    }

    private void SpawnLightToken()
    {
        EnsureDebugOverlayRoot();
        if (debugOverlayRoot == null || inputArea == null || canvasRect == null)
            return;

        float paddingX = Mathf.Min(50f, Mathf.Abs(inputArea.rect.width) * 0.2f);
        float paddingY = Mathf.Min(50f, Mathf.Abs(inputArea.rect.height) * 0.2f);
        Vector2 localPoint = new Vector2(
            UnityEngine.Random.Range(inputArea.rect.xMin + paddingX, inputArea.rect.xMax - paddingX),
            UnityEngine.Random.Range(inputArea.rect.yMin + paddingY, inputArea.rect.yMax - paddingY));

        if (!TryConvertInputLocalToCanvasLocal(localPoint, out Vector2 canvasLocalPoint))
            return;

        GameObject tokenObject = new GameObject("LightToken", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform tokenRect = tokenObject.GetComponent<RectTransform>();
        tokenRect.SetParent(debugOverlayRoot, false);
        tokenRect.anchorMin = new Vector2(0.5f, 0.5f);
        tokenRect.anchorMax = new Vector2(0.5f, 0.5f);
        tokenRect.pivot = new Vector2(0.5f, 0.5f);
        tokenRect.anchoredPosition = canvasLocalPoint;
        tokenRect.sizeDelta = lightVisualSize;
        tokenRect.SetAsLastSibling();

        Image tokenImage = tokenObject.GetComponent<Image>();
        tokenImage.sprite = GetDebugSprite();
        tokenImage.color = lightVisualColor;
        tokenImage.raycastTarget = false;

        activeLights.Add(new LightToken
        {
            rectTransform = tokenRect,
            localPoint = localPoint,
            remainingLifetime = Mathf.Max(0.5f, lightLifetimeSeconds)
        });
    }

    private bool TryCatchLight(Vector2 strokeLocalPoint)
    {
        for (int i = activeLights.Count - 1; i >= 0; i--)
        {
            LightToken token = activeLights[i];
            if (Vector2.Distance(token.localPoint, strokeLocalPoint) > lightCatchRadiusPixels)
                continue;

            bonusScore += Mathf.Max(1, lightBonusPoints);
            lightsCollectedThisStage++;
            UpdateBonusScoreText();
            
            if (lightsCollectedThisStage >= totalLightsRequired && !lightRayActive)
            {
                lightRayActive = true;
                SetFeedback($"Все {totalLightsRequired} солнышек собраны! Луч света активирован - мазки становятся ярче!");
            }
            else
            {
                SetFeedback($"Поймала свет! ({lightsCollectedThisStage}/{totalLightsRequired})");
            }
            
            RemoveLightTokenAt(i);
            return true;
        }
        return false;
    }

    private void ClearLightTokens()
    {
        for (int i = activeLights.Count - 1; i >= 0; i--)
            RemoveLightTokenAt(i);

        activeLights.Clear();
    }

    private void RemoveLightTokenAt(int index)
    {
        if (index < 0 || index >= activeLights.Count)
            return;

        LightToken token = activeLights[index];
        if (token.rectTransform != null)
            Destroy(token.rectTransform.gameObject);

        activeLights.RemoveAt(index);
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(Mathf.Max(0f, stageTimeRemaining));
        timerText.text = $"Время: {seconds}";
    }

    private void SetNextStageButtonState(bool isReady)
    {
        if (nextStageButton == null)
            return;

        bool isLastStage = stages == null || currentStageIndex >= stages.Length - 1;
        nextStageButton.interactable = isReady && !isLastStage;
    }

    private bool IsCurrentColorAllowed()
    {
        StageDefinition stage = GetCurrentStage();
        if (stage == null || stage.allowedPaletteIndices == null || stage.allowedPaletteIndices.Length == 0)
            return true;

        for (int i = 0; i < stage.allowedPaletteIndices.Length; i++)
        {
            if (stage.allowedPaletteIndices[i] == currentPaletteIndex)
                return true;
        }

        return false;
    }

    private StageDefinition GetCurrentStage()
    {
        if (stages == null || stages.Length == 0)
            return null;

        int index = Mathf.Clamp(currentStageIndex, 0, stages.Length - 1);
        return stages[index];
    }

    private void UpdatePaletteVisuals()
    {
        if (paletteSwatches == null)
            return;

        StageDefinition stage = GetCurrentStage();

        for (int i = 0; i < paletteSwatches.Length; i++)
        {
            Image swatch = paletteSwatches[i];
            if (swatch == null)
                continue;

            bool hasColor = paletteColors != null && i < paletteColors.Length;
            if (hasColor)
                swatch.color = ApplyPaletteVisualAlpha(paletteColors[i], stage, i);

            RectTransform swatchRect = swatch.rectTransform;
            if (swatchRect != null)
            {
                float scale = i == currentPaletteIndex ? selectedPaletteScale : unselectedPaletteScale;
                swatchRect.localScale = Vector3.one * scale;
            }
        }
    }

    private Color ApplyPaletteVisualAlpha(Color color, StageDefinition stage, int paletteIndex)
    {
        bool allowed = true;

        if (stage != null && stage.allowedPaletteIndices != null && stage.allowedPaletteIndices.Length > 0)
            allowed = Array.IndexOf(stage.allowedPaletteIndices, paletteIndex) >= 0;

        color.a = allowed ? activePaletteAlpha : inactivePaletteAlpha;
        return color;
    }

    private Color GetCurrentPaintColor(float opacity)
    {
        Color baseColor = fallbackStampColor;

        if (paletteColors != null && paletteColors.Length > 0 && currentPaletteIndex >= 0 && currentPaletteIndex < paletteColors.Length)
            baseColor = paletteColors[currentPaletteIndex];

        // Применяем усиление, если луч активирован
        if (lightRayActive)
        {
            // Увеличиваем насыщенность
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            s = Mathf.Clamp01(s + colorSaturationBonus);
            v = Mathf.Clamp01(v + colorBrightnessBonus);
            baseColor = Color.HSVToRGB(h, s, v);
            baseColor.a = Mathf.Clamp01(baseColor.a * opacity);
        }
        else
        {
            baseColor.a = Mathf.Clamp01(baseColor.a * opacity);
        }

        return baseColor;
    }

    private void ResolveCamera()
    {
        if (targetCanvas == null)
        {
            uiCamera = null;
            canvasRect = null;
            return;
        }

        canvasRect = targetCanvas.transform as RectTransform;
        uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : targetCanvas.worldCamera;
    }

    private bool TryGetLocalPoint(Vector2 screenPosition, out Vector2 localPoint)
    {
        ResolveCamera();

        if (inputArea == null)
        {
            localPoint = default;
            return false;
        }

        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            inputArea,
            screenPosition,
            uiCamera,
            out localPoint);

        if (!converted)
            return false;

        return inputArea.rect.Contains(localPoint);
    }

    private void EnsurePaintLayer()
    {
        if (paintLayer != null && paintTexture != null)
            return;

        EnsureDebugOverlayRoot();
        if (debugOverlayRoot == null)
            return;

        Transform existing = debugOverlayRoot.Find("StrokePaintLayer");
        if (existing != null)
            paintLayer = existing.GetComponent<RawImage>();

        if (paintLayer == null)
        {
            GameObject layerObject = new GameObject("StrokePaintLayer", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            RectTransform layerRect = layerObject.GetComponent<RectTransform>();
            layerRect.SetParent(debugOverlayRoot, false);
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            layerRect.SetAsLastSibling();

            paintLayer = layerObject.GetComponent<RawImage>();
            paintLayer.raycastTarget = false;
        }

        if (paintTexture == null)
        {
            int width = Mathf.Max(128, canvasSize.x);
            int height = Mathf.Max(128, canvasSize.y);
            paintTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            paintTexture.wrapMode = TextureWrapMode.Clamp;
            paintTexture.filterMode = FilterMode.Bilinear;
            paintLayer.texture = paintTexture;
            InitializeClearPixels();
        }
    }

    private bool TryConvertInputLocalToCanvasLocal(Vector2 inputLocalPoint, out Vector2 canvasLocalPoint)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, inputArea.TransformPoint(inputLocalPoint));
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out canvasLocalPoint);
    }

    private void EnsureDebugOverlayRoot()
    {
        if (debugOverlayRoot != null)
            return;

        ResolveCamera();
        if (canvasRect == null)
            return;

        Transform existing = canvasRect.Find("LiveContourDebugOverlay");
        if (existing != null)
        {
            debugOverlayRoot = existing as RectTransform;
            return;
        }

        GameObject overlayObject = new GameObject("LiveContourDebugOverlay", typeof(RectTransform));
        debugOverlayRoot = overlayObject.GetComponent<RectTransform>();
        debugOverlayRoot.SetParent(canvasRect, false);
        debugOverlayRoot.anchorMin = Vector2.zero;
        debugOverlayRoot.anchorMax = Vector2.one;
        debugOverlayRoot.offsetMin = Vector2.zero;
        debugOverlayRoot.offsetMax = Vector2.zero;
        debugOverlayRoot.SetAsLastSibling();
    }

    private void InitializeClearPixels()
    {
        if (paintTexture == null)
            return;

        clearPixels = new Color[paintTexture.width * paintTexture.height];
        Color transparent = new Color(0f, 0f, 0f, 0f);

        for (int i = 0; i < clearPixels.Length; i++)
            clearPixels[i] = transparent;
    }

    private void PlaceStamp(Vector2 localPoint)
    {
        EnsurePaintLayer();
        if (paintTexture == null || inputArea == null)
            return;

        Vector2 jitteredLocalPoint = localPoint + UnityEngine.Random.insideUnitCircle * positionJitterPixels;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, inputArea.TransformPoint(jitteredLocalPoint));
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 canvasLocalPoint))
            return;

        float normalizedX = Mathf.InverseLerp(-canvasRect.rect.width * 0.5f, canvasRect.rect.width * 0.5f, canvasLocalPoint.x);
        float normalizedY = Mathf.InverseLerp(-canvasRect.rect.height * 0.5f, canvasRect.rect.height * 0.5f, canvasLocalPoint.y);

        int centerX = Mathf.RoundToInt(normalizedX * (paintTexture.width - 1));
        int centerY = Mathf.RoundToInt(normalizedY * (paintTexture.height - 1));
        float scale = baseStampScale * UnityEngine.Random.Range(1f - scaleJitter, 1f + scaleJitter);
        float rotation = UnityEngine.Random.Range(-rotationJitterDegrees, rotationJitterDegrees);
        float opacity = UnityEngine.Random.Range(1f - opacityJitter, 1f);
        Color paintColor = GetCurrentPaintColor(opacity);

        if (stampTexture != null && TryDrawStampTexture(centerX, centerY, scale, rotation, paintColor))
            return;

        DrawFallbackEllipse(centerX, centerY, scale, paintColor);
    }

    private bool TryDrawStampTexture(int centerX, int centerY, float scale, float rotationDegrees, Color paintColor)
    {
        if (!TryCacheStampPixels())
            return false;

        int width = Mathf.Max(1, Mathf.RoundToInt(cachedStampWidth * scale));
        int height = Mathf.Max(1, Mathf.RoundToInt(cachedStampHeight * scale));
        int startX = centerX - width / 2;
        int startY = centerY - height / 2;
        float radians = rotationDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        float invWidth = 1f / Mathf.Max(1, width);
        float invHeight = 1f / Mathf.Max(1, height);

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
                    source.a * paintColor.a);

                Color existing = paintTexture.GetPixel(targetX, targetY);
                Color blended = Color.Lerp(existing, tinted, tinted.a);
                blended.a = Mathf.Max(existing.a, tinted.a);
                paintTexture.SetPixel(targetX, targetY, blended);
            }
        }

        paintDirty = true;
        return true;
    }

    private void DrawFallbackEllipse(int centerX, int centerY, float scale, Color paintColor)
    {
        float radiusX = Mathf.Max(1f, fallbackStampSize.x * 0.5f * scale);
        float radiusY = Mathf.Max(1f, fallbackStampSize.y * 0.5f * scale);

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
                float shape = 1f - Mathf.Clamp01(dx * dx + dy * dy);

                if (shape <= 0f)
                    continue;

                Color existing = paintTexture.GetPixel(x, y);
                float alpha = shape * paintColor.a;
                Color blended = Color.Lerp(existing, paintColor, alpha);
                blended.a = Mathf.Max(existing.a, alpha);
                paintTexture.SetPixel(x, y, blended);
            }
        }

        paintDirty = true;
    }

    private void CreateDebugMarker(Vector2 localPoint)
    {
        EnsureDebugOverlayRoot();
        if (!showDebugMarkers || inputArea == null || debugOverlayRoot == null || canvasRect == null)
            return;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, inputArea.TransformPoint(localPoint));
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 canvasLocalPoint))
            return;

        GameObject markerObject = new GameObject("DebugStamp", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform markerRect = markerObject.GetComponent<RectTransform>();
        markerRect.SetParent(debugOverlayRoot, false);
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        markerRect.anchoredPosition = canvasLocalPoint;
        markerRect.sizeDelta = fallbackStampSize;
        markerRect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-20f, 20f));
        markerRect.SetAsLastSibling();

        Image markerImage = markerObject.GetComponent<Image>();
        markerImage.sprite = GetDebugSprite();
        markerImage.color = new Color(1f, 0f, 0f, 0.9f);
        markerImage.raycastTarget = false;
    }

    private void ClearDebugMarkers()
    {
        if (debugOverlayRoot == null)
            return;

        for (int i = debugOverlayRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = debugOverlayRoot.GetChild(i);
            if (child != null && child.name == "DebugStamp")
                Destroy(child.gameObject);
        }
    }

    private static Sprite GetDebugSprite()
    {
        if (debugSprite != null)
            return debugSprite;

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixels(new[]
        {
            Color.white, Color.white,
            Color.white, Color.white
        });
        texture.Apply(false, true);

        debugSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));

        return debugSprite;
    }

    private bool TryCacheStampPixels()
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
        catch (UnityException)
        {
            if (!loggedUnreadableTextureWarning)
            {
                Debug.LogWarning("LiveContourMiniGame: stampTexture is not readable. Enable Read/Write in the texture import settings or the fallback brush will be used.");
                loggedUnreadableTextureWarning = true;
            }

            cachedStampPixels = null;
            cachedStampWidth = 0;
            cachedStampHeight = 0;
            cachedStampSource = null;
            return false;
        }
    }

    private void FlushPaint()
    {
        if (!paintDirty || paintTexture == null)
            return;

        paintTexture.Apply(false);
        paintDirty = false;
    }

    private void EnsureDefaultPalette()
    {
        if (paletteColors != null && paletteColors.Length > 0)
            return;

        paletteColors = new[]
        {
            Hex("4C430A"),
            Hex("665505"),
            Hex("7B4A0B"),
            Hex("13121A"),
            Hex("322726"),
            Hex("D81A08"),
            Hex("F04915"),
            Hex("EEE8E3"),
            Hex("CFC9CD"),
            Hex("A8A6B0"),
            Hex("E9B28D"),
            Hex("C98A66"),
            Hex("E58E4A"),
            Hex("A56A1E"),
            Hex("AFC5D5")
        };
    }

    private void EnsureDefaultStages()
    {
        if (stages != null && stages.Length > 0)
            return;

        stages = new[]
        {
            new StageDefinition
            {
                title = "Фон",
                hint = "Сначала собери общий фон. Работай крупно и спокойно.",
                requiredStrokeCount = 12,
                allowedPaletteIndices = new[] { 0, 1, 2 }
            },
            new StageDefinition
            {
                title = "Красная сцена",
                hint = "Теперь положи яркую нижнюю плоскость.",
                requiredStrokeCount = 10,
                allowedPaletteIndices = new[] { 5, 6 }
            },
            new StageDefinition
            {
                title = "Пиджак и темные массы",
                hint = "Собери крупные темные формы фигуры.",
                requiredStrokeCount = 14,
                allowedPaletteIndices = new[] { 3, 4 }
            },
            new StageDefinition
            {
                title = "Красные акценты",
                hint = "Добавь ритм красными акцентами.",
                requiredStrokeCount = 8,
                allowedPaletteIndices = new[] { 5, 6 }
            },
            new StageDefinition
            {
                title = "Лицо и руки",
                hint = "Теперь теплые телесные оттенки.",
                requiredStrokeCount = 8,
                allowedPaletteIndices = new[] { 10, 11 }
            },
            new StageDefinition
            {
                title = "Волосы и теплые детали",
                hint = "Добавь теплые рыжие и охристые места.",
                requiredStrokeCount = 6,
                allowedPaletteIndices = new[] { 12, 13 }
            },
            new StageDefinition
            {
                title = "Петух",
                hint = "Собери светлую массу петуха и не забудь про холодные тени.",
                requiredStrokeCount = 16,
                allowedPaletteIndices = new[] { 7, 8, 9 }
            },
            new StageDefinition
            {
                title = "Финальные детали",
                hint = "Проверь шляпу, рубашку, обувь и мелкие контрасты.",
                requiredStrokeCount = 8,
                allowedPaletteIndices = new[] { 3, 7, 13, 14 }
            }
        };
    }

    private void EnsureLightRayDisplay()
    {
        if (lightRayDisplay != null)
            return;

        EnsureDebugOverlayRoot();
        if (debugOverlayRoot == null)
            return;

        GameObject rayObject = new GameObject("LightRay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rayRect = rayObject.GetComponent<RectTransform>();
        rayRect.SetParent(debugOverlayRoot, false);
        rayRect.anchorMin = new Vector2(0.5f, 0.5f);
        rayRect.anchorMax = new Vector2(0.5f, 0.5f);
        rayRect.pivot = new Vector2(0.5f, 0.5f);
        rayRect.sizeDelta = Vector2.one * lightRaySize;
        rayRect.SetAsFirstSibling();

        Image rayImage = rayObject.GetComponent<Image>();
        rayImage.sprite = GetDebugSprite();
        rayImage.color = lightRayColor;
        rayImage.raycastTarget = false;

        lightRayDisplay = rayRect;
    }

    private void UpdateLightRayDisplay()
    {
        if (!lightRayActive || lightRayDisplay == null || !TryGetLocalPoint(Input.mousePosition, out Vector2 localPoint))
        {
            if (lightRayDisplay != null)
                lightRayDisplay.gameObject.SetActive(false);
            return;
        }

        lightRayDisplay.gameObject.SetActive(true);

        if (!TryConvertInputLocalToCanvasLocal(localPoint, out Vector2 canvasLocalPoint))
        {
            lightRayDisplay.gameObject.SetActive(false);
            return;
        }

        lightRayDisplay.anchoredPosition = canvasLocalPoint;
        
        // Пульсирующий эффект для луча
        float pulse = 0.85f + Mathf.Sin(Time.time * 5f) * 0.15f;
        lightRayDisplay.localScale = Vector3.one * pulse;
    }

    private static Color Hex(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
            return color;

        return Color.white;
    }
}

