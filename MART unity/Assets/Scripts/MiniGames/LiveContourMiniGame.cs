using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LiveContourMiniGame : MonoBehaviour
{
    private const string SavedArtworkPointsKey = "LiveContourMiniGame.LastCompletedPoints";
    private const string SavedArtworkCompletedKey = "LiveContourMiniGame.HasCompletedSession";
    private const string MiniGameCardObjectName = "Mini game2";
    private const string MiniGamesScreenObjectName = "Mini games";
    private const string MiniGameScreenObjectName = "Mini game screen 2";
    private const string PrologueScreenObjectName = "The Prologue";
    private const string FinalScreenObjectName = "The final screen";
    private const string HomeScreenObjectName = "Home";

    [Serializable]
    private class StageDefinition
    {
        public string title;
        [TextArea(2, 4)] public string hint;
        [Min(1)] public int requiredStrokeCount = 8;
        public int[] allowedPaletteIndices;
        public PaintRegionMode paintRegionMode = PaintRegionMode.Anywhere;
    }

    [Serializable]
    private class ArtMovementDefinition
    {
        public string id;
        public string resultTitle;
        public Sprite referenceSprite;
        public Sprite[] exampleSprites = new Sprite[3];
        public Texture2D stampTexture;
        public bool useDotStamps;
        public Color[] paletteColors;
        public StageDefinition[] stages;
        public Vector2 fallbackStampSize = new Vector2(72f, 28f);
        public float dragSpacingPixels = 22f;
        public float baseStampScale = 0.1f;
        public float scaleJitter = 0.1f;
        public float rotationJitterDegrees = 35f;
        public float opacityJitter = 0.06f;
        public float strokeOverlapBlend = 0.48f;
        public float strokeColorJitter = 0.035f;
        public Vector2 figureZoneCenterUv = new Vector2(0.5f, 0.5f);
        public Vector2 figureZoneRadiusUv = new Vector2(0.24f, 0.34f);
        [TextArea(4, 12)] public string description;
    }

    private enum PaintRegionMode
    {
        Anywhere = 0,
        DarkBackgroundOnly = 1,
        BottomBandOnly = 2,
        FigureOnly = 3,
        BrightAreasOnly = 4
    }

    [Serializable]
    private class LightToken
    {
        public RectTransform rectTransform;
        public Vector2 localPoint;
        public float remainingLifetime;
    }

    private static Sprite debugSprite;

    private readonly LiveContourStrokePainter strokePainter = new LiveContourStrokePainter();

    [Header("Input")]
    [SerializeField] private RectTransform inputArea;
    [SerializeField] private Canvas targetCanvas;

    [Header("Visual")]
    [SerializeField] private Texture2D stampTexture;
    [SerializeField] private Color fallbackStampColor = new Color(0.08f, 0.08f, 0.08f, 1f);
    [SerializeField] private Vector2Int canvasSize = new Vector2Int(512, 512);
    [SerializeField] [Range(0.04f, 0.4f)] private float canvasTintAlpha = 0.12f;
    [SerializeField] private Vector2 fallbackStampSize = new Vector2(72f, 28f);
    [SerializeField] private float dragSpacingPixels = 22f;
    [SerializeField] private float baseStampScale = 0.1f;
    [SerializeField] private float scaleJitter = 0.1f;
    [SerializeField] private float rotationJitterDegrees = 35f;
    [SerializeField] private float opacityJitter = 0.06f;
    [SerializeField] private float strokeOverlapBlend = 0.48f;
    [SerializeField] private float strokeColorJitter = 0.035f;
    [SerializeField] private float positionJitterPixels = 0f;
    [SerializeField] private bool showDebugMarkers;
    [SerializeField] private bool verboseInputLogging = true;

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

    [Header("Paint Regions")]
    [SerializeField] private Image referenceArtwork;
    [SerializeField] private float darkBackgroundLuminanceMax = 0.62f;
    [SerializeField] private float figureFillLuminanceMin = 0.78f;
    [SerializeField] private float brightAreaLuminanceMin = 0.52f;
    [SerializeField] private float regionSampleRadiusPixels = 14f;
    [SerializeField] private float blockFeedbackCooldownSeconds = 0.75f;
    [SerializeField] private int wrongAttemptsBeforeRegionHint = 1;
    [SerializeField] private float idleSecondsBeforeRegionHint = 5f;
    [SerializeField] private Color regionHintColor = new Color(0.25f, 0.9f, 0.4f, 0.38f);
    [SerializeField] private Vector2 figureZoneCenterUv = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 figureZoneRadiusUv = new Vector2(0.24f, 0.34f);

    [Header("HUD")]
    [SerializeField] private TMP_Text stageTitleText;
    [SerializeField] private TMP_Text stageHintText;
    [SerializeField] private TMP_Text strokeProgressText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text bonusScoreText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button nextStageButton;
    [SerializeField] private TMP_Text miniGameStatusText;
    [SerializeField] private RectTransform miniGameCard;
    [SerializeField] private GameObject miniGameScreen;
    [SerializeField] private GameObject miniGamesScreen;
    [SerializeField] private GameObject prologueScreen;
    [SerializeField] private GameObject finalScreen;
    [SerializeField] private GameObject homeScreen;
    [SerializeField] private TMP_Text finalResultText;

    [Header("Art Movements")]
    [SerializeField] private ArtMovementDefinition[] artMovements;
    [SerializeField] private Sprite impressionismReferenceSprite;
    [SerializeField] private Sprite impressionismExampleSprite1;
    [SerializeField] private Sprite impressionismExampleSprite2;
    [SerializeField] private Sprite impressionismExampleSprite3;
    [SerializeField] private Sprite pointillismReferenceSprite;
    [SerializeField] private Sprite pointillismExampleSprite1;
    [SerializeField] private Sprite pointillismExampleSprite2;
    [SerializeField] private Sprite pointillismExampleSprite3;
    [SerializeField] private Texture2D impressionismStampTexture;
    [SerializeField] private Texture2D pointillismStampTexture;

    [Header("Art Movement — Final Screen")]
    [SerializeField] private GameObject movementContentRoot;
    [SerializeField] private TMP_Text movementDescriptionText;
    [SerializeField] private Button finalScreenActionButton;
    [SerializeField] private TMP_Text finalScreenActionButtonText;

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
    [SerializeField] private Vector2 lightVisualSize = new Vector2(88f, 88f);
    [SerializeField] private Color lightVisualColor = new Color(1f, 0.88f, 0.35f, 0.95f);

    [Header("Light Ray Bonus (Golden Ray)")]
    [SerializeField] private float lightRaySize = 120f;
    [SerializeField] private Color lightRayColor = new Color(1f, 0.95f, 0.5f, 0.6f);
    [SerializeField] private float colorSaturationBonus = 0.4f;
    [SerializeField] private float colorBrightnessBonus = 0.3f;

    private Camera uiCamera;
    private Vector2 lastStampLocalPosition;
    private Vector2 lastScreenPosition;
    private float lastPaintU;
    private float lastPaintV;
    private bool wasPointerHeld;
    private RectTransform canvasRect;
    private RectTransform debugOverlayRoot;
    private int currentPaletteIndex;
    private int currentStageIndex;
    private int currentStageStrokeCount;
    private bool currentStrokeAlreadyCounted;
    private float stageTimeRemaining;
    private float idleTime;
    private bool idleHintShown;
    private bool stageExpired;
    private float nextLightSpawnDelay;
    private int lightsCollectedThisStage;
    private bool lightRayActive;
    private Vector2 lightRayPosition;
    private RectTransform lightRayDisplay;
    private readonly List<LightToken> activeLights = new List<LightToken>();
    private readonly HashSet<int> usedPaletteIndices = new HashSet<int>();
    private int totalRegisteredStrokes;
    private int completedStagesInTimeCount;
    private bool currentStageProgressCommitted;
    private bool currentStageCompletedInTime;
    private int totalLightsSpawnedThisSession;
    private float currentAttemptPoints;
    private float lastCompletedPoints;
    private bool hasSavedCompletedSession;
    private bool sessionFinished;
    private bool lightsBonusEarned;
    private bool isShowingFinalScreen;
    private bool feedbackIsWarning;
    private Color defaultFeedbackColor = Color.black;
    private Color defaultStageHintColor = Color.black;
    private int currentMovementIndex;
    private bool useDotStamps;
    private Image[] movementExampleImages;
    private Color[] cachedReferencePixels;
    private int cachedReferenceWidth;
    private int cachedReferenceHeight;
    private bool referencePixelsReady;
    private float lastBlockFeedbackTime = -999f;
    private string lastBlockFeedbackMessage;
    private int wrongRegionAttempts;
    private bool regionHintActive;
    private RawImage regionHintLayer;
    private Texture2D regionHintTexture;
    private bool[] stageAllowMask;
    private bool stageAllowMaskReady;
    private int stageMaskWidth;
    private int stageMaskHeight;
    private const int StageMaskResolution = 128;
    private static readonly Vector2[] RegionSampleOffsets =
    {
        Vector2.zero,
        new Vector2(-1f, 0f),
        new Vector2(1f, 0f),
        new Vector2(0f, -1f),
        new Vector2(0f, 1f)
    };
    private static readonly Color WarningFeedbackColor = new Color(0.86f, 0.12f, 0.12f, 1f);
    private const string WarningFeedbackHex = "#DB1F1F";
    private const string WrongColorSelectionMessage =
        "Этот цвет не подходит для этапа. Выбери один из подсвеченных цветов на палитре.";
    private const string WrongColorPaintMessage =
        "Этим цветом сейчас рисовать нельзя. Выбери подсвеченный цвет на палитре.";
    private const string WrongRegionPaintMessage =
        "Здесь сейчас рисовать нельзя. Смотри подсказку этапа и закрашивай нужную часть.";

    private void Awake()
    {
        if (inputArea == null)
            inputArea = transform as RectTransform;

        Image areaImage = GetComponent<Image>();
        if (areaImage != null)
            areaImage.raycastTarget = true;

        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        EnsureDefaultPalette();
        EnsureDefaultStages();
        EnsureDefaultArtMovements();
        ResolveArtMovementSprites();
        ResolveCamera();
        EnsureDebugOverlayRoot();
        EnsureReferenceArtwork();
        ApplyCurrentMovement(false);
        currentPaletteIndex = Mathf.Clamp(defaultPaletteIndex, 0, Mathf.Max(0, paletteColors.Length - 1));
        lightsCollectedThisStage = 0;
        lightRayActive = false;
        LoadSavedSession();
        EnsureMiniGameStatusText();
        EnsureScreenReferences();
        EnsureHudReferences();
        EnsureReferenceArtwork();
        EnsureFinalResultText();
        EnsureFinalScreenContentReferences();
        HideFinalScreen();
        UpdateMiniGameStatusText();
        UpdateBonusScoreText();
        ResetSessionState();
        UpdatePaletteVisuals();
        EnsureLightRayDisplay();
        EnsureRegionHintLayer();

        if (feedbackText != null)
            defaultFeedbackColor = feedbackText.color;

        if (stageHintText != null)
            defaultStageHintColor = stageHintText.color;
    }

    private void OnEnable()
    {
        if (verboseInputLogging)
            Debug.Log($"[LiveContour] OnEnable | activeInHierarchy={gameObject.activeInHierarchy} | parent={(transform.parent != null ? transform.parent.name : "null")}");

        isShowingFinalScreen = false;
        sessionFinished = false;
        stageExpired = false;

        EnsureGameConfiguration();
        EnsureScreenReferences();
        EnsureHudReferences();
        EnsureReferenceArtwork();
        EnsureStrokePainter();
        EnsureHudDoesNotBlockPaint();
        ApplyCurrentMovement(false);
        EnsurePlayableViewVisible();
        strokePainter.SetVisible(true);
        EnsureAllowedPaletteSelected();
        UpdatePaletteVisuals();
        UpdateStageTexts();
        UpdateProgressText();
        UpdateFeedbackForCurrentColor();
        BuildStageAllowMask();
        RebuildRegionHintTexture();
        UpdateRegionHintDisplay();
        StartCoroutine(SnapPlayableNextFrame());
    }

    private void OnDisable()
    {
        if (verboseInputLogging)
            Debug.Log($"[LiveContour] OnDisable | isShowingFinalScreen={isShowingFinalScreen} | sessionFinished={sessionFinished}");

        if (isShowingFinalScreen)
        {
            strokePainter.SetVisible(false);
            return;
        }

        CloseSession();
    }

    private void LateUpdate()
    {
        if (!isActiveAndEnabled || sessionFinished || isShowingFinalScreen)
            return;

        if (miniGameScreen == null || !miniGameScreen.activeInHierarchy)
            return;

        strokePainter.RefreshLayout();
    }

    private void Update()
    {
        if (verboseInputLogging && Time.unscaledTime - lastHeartbeatTime > 2f)
        {
            lastHeartbeatTime = Time.unscaledTime;
            Debug.Log($"[LiveContour] Update HEARTBEAT | timeRemaining={stageTimeRemaining:F1} | stageExpired={stageExpired} | stage={(GetCurrentStage() != null)} | painterReady={strokePainter.IsReady}");
        }

        UpdateTimers();
        UpdateLightTokens();
        UpdateLightRayDisplay();
        UpdateRegionHintDisplay();

        bool freshMouseDown = Input.GetMouseButtonDown(0);
        if (freshMouseDown)
            LogInputDiagnostics();

        if (!CanAcceptPaintInput())
        {
            wasPointerHeld = false;
            return;
        }

        if (!TryGetActivePointer(out Vector2 screenPosition, out bool pointerHeld) || !pointerHeld)
        {
            wasPointerHeld = false;
            lastBlockFeedbackMessage = null;
            return;
        }

        // Не рисуем, если указатель над интерактивным элементом (образец палитры,
        // кнопка): иначе клик по цвету ставит штрих на холсте. Проверяем именно
        // Selectable, а не любой raycast-таргет, потому что фон Content тоже ловит
        // raycast и иначе заблокировал бы рисование по всей картине.
        if (IsPointerOverBlockingUi(screenPosition))
        {
            wasPointerHeld = false;
            return;
        }

        if (!strokePainter.TryScreenToStrokeUv(screenPosition, out float paintU, out float paintV, out Vector2 localPoint))
        {
            if (verboseInputLogging && freshMouseDown)
                Debug.LogWarning($"[LiveContour] TryScreenToStrokeUv FAILED at screen={screenPosition}. Painter.IsReady={strokePainter.IsReady}, referenceArtwork={(referenceArtwork != null)}");
            return;
        }

        if (!wasPointerHeld)
        {
            BeginStroke(localPoint, paintU, paintV, screenPosition);
            return;
        }

        ContinueStroke(localPoint, paintU, paintV, screenPosition);
    }

    public void OpenMiniGameScreen()
    {
        EnsureScreenReferences();

        isShowingFinalScreen = false;
        sessionFinished = false;
        stageExpired = false;
        wasPointerHeld = false;

        if (homeScreen != null)
            homeScreen.SetActive(false);

        if (miniGamesScreen != null)
            miniGamesScreen.SetActive(false);

        if (prologueScreen != null)
            prologueScreen.SetActive(false);

        HideFinalScreen();

        if (miniGameScreen != null)
        {
            miniGameScreen.SetActive(true);

            Transform parent = miniGameScreen.transform.parent;
            if (parent != null)
                miniGameScreen.transform.SetAsLastSibling();
        }

        InitializeMiniGameSession();
    }

    public void OnMiniGameScreenOpened()
    {
        InitializeMiniGameSession();
    }

    private void InitializeMiniGameSession()
    {
        isShowingFinalScreen = false;
        sessionFinished = false;
        stageExpired = false;
        wasPointerHeld = false;
        EnsureGameConfiguration();
        EnsureReferenceArtwork();
        EnsureStrokePainter();
        EnsureHudDoesNotBlockPaint();
        ApplyCurrentMovement(false);
        ResetSessionState();
        EnsurePlayableViewVisible();
        strokePainter.RefreshLayout();
        strokePainter.SetVisible(true);
        EnsureAllowedPaletteSelected();
        UpdatePaletteVisuals();
        StartCoroutine(SnapPlayableNextFrame());
    }

    public void HandlePaintPointerDown(PointerEventData eventData) { }

    public void HandlePaintPointerDrag(PointerEventData eventData) { }

    public void HandlePaintPointerUp(PointerEventData eventData)
    {
        wasPointerHeld = false;
        lastBlockFeedbackMessage = null;
    }

    private void PlaceStrokeAtUv(float paintU, float paintV)
    {
        EnsureStrokePainter();
        float opacity = UnityEngine.Random.Range(1f - opacityJitter, 1f);
        Color paintColor = ApplyStrokeColorJitter(GetCurrentPaintColor(opacity));
        strokePainter.PaintAtUv(paintU, paintV, paintColor, BuildBrushSettings());

        if (verboseInputLogging && Time.unscaledTime - lastPaintLogTime > 0.5f)
        {
            lastPaintLogTime = Time.unscaledTime;
            Debug.Log($"[LiveContour] PAINTED at uv=({paintU:F2},{paintV:F2}) color={paintColor} a={paintColor.a:F2}. Painter.IsReady={strokePainter.IsReady}, stamp={(stampTexture != null)}");
        }
    }

    private float lastPaintLogTime = -1f;
    private float lastHeartbeatTime = -1f;

    private void LogInputDiagnostics()
    {
        if (!verboseInputLogging)
            return;

        string gate;
        if (!isActiveAndEnabled)
            gate = "BLOCKED: component not active/enabled";
        else if (sessionFinished)
            gate = "BLOCKED: sessionFinished == true";
        else if (isShowingFinalScreen)
            gate = "BLOCKED: final screen is showing";
        else if (miniGameScreen == null)
            gate = "BLOCKED: miniGameScreen reference is NULL";
        else if (!miniGameScreen.activeInHierarchy)
            gate = "BLOCKED: miniGameScreen not active in hierarchy";
        else if (!strokePainter.IsReady && referenceArtwork == null)
            gate = "BLOCKED: painter not ready AND referenceArtwork is NULL";
        else
            gate = "OK: input accepted, should paint";

        Debug.Log($"[LiveContour] MouseDown gate => {gate} | painterReady={strokePainter.IsReady} | referenceArtwork={(referenceArtwork != null)} | currentStage={(GetCurrentStage() != null)} | paletteColors={(paletteColors != null ? paletteColors.Length : 0)}");
    }

    private LiveContourStrokePainter.BrushSettings BuildBrushSettings()
    {
        return new LiveContourStrokePainter.BrushSettings
        {
            StampTexture = stampTexture,
            FallbackStampSize = fallbackStampSize,
            BaseStampScale = baseStampScale,
            ScaleJitter = scaleJitter,
            RotationJitterDegrees = rotationJitterDegrees,
            OpacityJitter = 0f,
            StrokeOverlapBlend = strokeOverlapBlend,
            UseDotStamps = useDotStamps
        };
    }

    private void EnsureStrokePainter()
    {
        if (inputArea == null)
            inputArea = transform as RectTransform;

        EnsureScreenReferences();
        if (referenceArtwork == null)
            EnsureReferenceArtwork();

        strokePainter.Configure(inputArea, referenceArtwork, canvasSize, canvasTintAlpha);
        // RefreshLayout здесь НЕ вызываем: EnsureStrokePainter дёргается из OnEnable,
        // а привязка к картине меняет иерархию (репэрент) — во время активации Unity
        // это запрещает. Привязку делает LateUpdate, когда активация уже завершена.

        if (referenceArtwork != null)
            referenceArtwork.raycastTarget = false;
    }

    private IEnumerator SnapPlayableNextFrame()
    {
        yield return null;
        EnsurePlayableViewVisible();
        strokePainter.RefreshLayout();
        EnsureHudDoesNotBlockPaint();
        EnsureStrokePainter();
    }

    private bool CanAcceptPaintInput()
    {
        if (!isActiveAndEnabled || sessionFinished || isShowingFinalScreen)
            return false;

        if (miniGameScreen != null && !miniGameScreen.activeInHierarchy)
            return false;

        return strokePainter.IsReady || referenceArtwork != null;
    }

    public void SelectPaletteColor(int paletteIndex)
    {
        if (paletteColors == null || paletteColors.Length == 0)
            return;

        int requestedIndex = Mathf.Clamp(paletteIndex, 0, paletteColors.Length - 1);
        if (requestedIndex == currentPaletteIndex)
        {
            if (!IsCurrentColorAllowed())
                ShowWrongColorFeedback(WrongColorSelectionMessage);

            return;
        }

        int previousIndex = currentPaletteIndex;
        currentPaletteIndex = requestedIndex;
        if (!IsCurrentColorAllowed())
        {
            currentPaletteIndex = previousIndex;
            ShowWrongColorFeedback(WrongColorSelectionMessage);
            UpdatePaletteVisuals();
            return;
        }

        UpdatePaletteVisuals();
        UpdateFeedbackForCurrentColor();
    }

    public void NextStage()
    {
        if (stages == null || stages.Length == 0)
            return;

        if (IsLastStage())
        {
            CompleteSession();
            return;
        }

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
        isShowingFinalScreen = false;
        ClearSavedSession();
        UpdateMiniGameStatusText();
        HideFinalScreen();
        ApplyCurrentMovement(true);
        ShowMiniGameScreen();
        strokePainter.SetVisible(true);
        ResetSessionState();
    }

    public void CloseSession()
    {
        isShowingFinalScreen = false;
        wasPointerHeld = false;
        currentStrokeAlreadyCounted = false;
        lightRayActive = false;
        currentMovementIndex = 0;
        ResetSessionState();
        HideFinalScreen();
        strokePainter.SetVisible(false);
    }

    public void RestartFromFinalScreen()
    {
        HandleFinalScreenAction();
    }

    public void HandleFinalScreenAction()
    {
        if (HasNextMovement())
            StartNextMovement();
        else
            RestartMiniGame();
    }

    public void StartNextMovement()
    {
        if (!HasNextMovement())
            return;

        isShowingFinalScreen = false;
        currentMovementIndex++;
        ClearSavedSession();
        UpdateMiniGameStatusText();
        HideFinalScreen();
        ApplyCurrentMovement(true);
        ShowMiniGameScreen();
        strokePainter.SetVisible(true);
        ResetSessionState();
    }

    public void GoToMiniGames()
    {
        isShowingFinalScreen = false;
        wasPointerHeld = false;
        currentStrokeAlreadyCounted = false;
        lightRayActive = false;
        currentMovementIndex = 0;
        HideFinalScreen();
        ShowMiniGamesScreen();
        strokePainter.SetVisible(false);
        ApplyCurrentMovement(true);
        ResetSessionState();
    }

    public void ClearCanvas()
    {
        EnsureStrokePainter();
        strokePainter.Clear();
        ClearLightTokens();
        ClearDebugMarkers();
    }

    private void BeginStroke(Vector2 localPoint, float paintU, float paintV, Vector2 screenPosition)
    {
        idleTime = 0f;
        idleHintShown = false;
        bool caughtLight = TryCatchLight(localPoint);

        if (!caughtLight)
        {
            PlaceStrokeAtUv(paintU, paintV);
            CreateDebugMarker(localPoint);
            TryRegisterStroke();
        }

        lastStampLocalPosition = localPoint;
        lastScreenPosition = screenPosition;
        lastPaintU = paintU;
        lastPaintV = paintV;
        wasPointerHeld = true;
    }

    private void ContinueStroke(Vector2 localPoint, float paintU, float paintV, Vector2 screenPosition)
    {
        float distance = Vector2.Distance(lastStampLocalPosition, localPoint);
        if (distance < Mathf.Max(1f, dragSpacingPixels))
            return;

        Vector2 direction = (localPoint - lastStampLocalPosition).normalized;
        float travelled = dragSpacingPixels;

        while (travelled <= distance)
        {
            Vector2 point = lastStampLocalPosition + direction * travelled;
            float stepU = Mathf.Lerp(lastPaintU, paintU, travelled / distance);
            float stepV = Mathf.Lerp(lastPaintV, paintV, travelled / distance);
            bool caughtLight = TryCatchLight(point);
            if (!caughtLight)
            {
                PlaceStrokeAtUv(stepU, stepV);
                CreateDebugMarker(point);
                TryRegisterStroke();
            }

            travelled += dragSpacingPixels;
        }

        lastStampLocalPosition = localPoint;
        lastScreenPosition = screenPosition;
        lastPaintU = paintU;
        lastPaintV = paintV;
    }

    private void TryRegisterStroke()
    {
        StageDefinition stage = GetCurrentStage();
        if (stage == null)
            return;

        if (!IsCurrentColorAllowed())
        {
            ShowBlockedPaintFeedbackThrottled(WrongColorPaintMessage);
            return;
        }

        wrongRegionAttempts = 0;
        regionHintActive = false;
        UpdateRegionHintDisplay();
        currentStageStrokeCount++;
        totalRegisteredStrokes++;
        usedPaletteIndices.Add(currentPaletteIndex);
        UpdateProgressText();

        if (currentStageStrokeCount >= stage.requiredStrokeCount)
        {
            currentStageCompletedInTime = stageTimeRemaining > 0f;
            SetFeedback("Этап готов. Можно нажать \"Дальше\".");
            SetNextStageButtonState(true);
            stageExpired = true;

            if (IsLastStage())
                CompleteSession();
        }
        else
        {
            SetFeedback("Хорошо. Продолжай в этом цвете.");
        }
    }

    private bool CanPaintWithCurrentColor()
    {
        return GetCurrentStage() != null && IsCurrentColorAllowed();
    }

    private bool CanPaintAtScreen(Vector2 screenPosition, out string blockReason, out bool isColorBlock)
    {
        blockReason = null;
        isColorBlock = false;

        if (GetCurrentStage() == null)
            return false;

        if (!IsCurrentColorAllowed())
        {
            blockReason = WrongColorPaintMessage;
            isColorBlock = true;
            return false;
        }

        return true;
    }

    private bool CanPaintAtUv(float u, float v, out string blockReason, out bool isColorBlock)
    {
        blockReason = null;
        isColorBlock = false;

        if (GetCurrentStage() == null)
            return false;

        if (!IsCurrentColorAllowed())
        {
            blockReason = WrongColorPaintMessage;
            isColorBlock = true;
            return false;
        }

        return true;
    }

    private bool CanPaintAtPoint(Vector2 localPoint, out string blockReason, out bool isColorBlock)
    {
        blockReason = null;
        isColorBlock = false;

        if (!TryResolvePaintUv(localPoint, out float u, out float v))
            return false;

        return CanPaintAtUv(u, v, out blockReason, out isColorBlock);
    }

    private bool IsPaintLocationAllowed(Vector2 localPoint)
    {
        StageDefinition stage = GetCurrentStage();
        if (stage == null || stage.paintRegionMode == PaintRegionMode.Anywhere)
            return true;

        if (!TryResolvePaintUv(localPoint, out float u, out float v))
            return false;

        return IsAllowedAtUv(u, v, stage.paintRegionMode);
    }

    private bool TryResolvePaintUv(Vector2 localPoint, out float u, out float v)
    {
        u = 0f;
        v = 0f;

        if (referenceArtwork != null
            && TryGetSpriteUvFromImageLocal(localPoint, out u, out v))
            return true;

        if (referenceArtwork != null)
        {
            Rect rect = referenceArtwork.rectTransform.rect;
            u = Mathf.Clamp01(Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x));
            v = Mathf.Clamp01(Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y));
            return true;
        }

        return TryGetSpriteUvFromInputPoint(localPoint, out u, out v);
    }

    private bool IsAllowedAtUv(float u, float v, PaintRegionMode mode)
    {
        if (mode == PaintRegionMode.Anywhere)
            return true;

        float luminance = SampleAverageLuminanceAtUv(u, v);
        return EvaluateAllowAtUv(u, v, luminance, mode);
    }

    private float SampleAverageLuminanceAtUv(float u, float v)
    {
        if (!referencePixelsReady)
            return 0.5f;

        float sum = 0f;
        const float sampleRadiusUv = 0.018f;

        for (int i = 0; i < RegionSampleOffsets.Length; i++)
        {
            Vector2 offset = RegionSampleOffsets[i] * sampleRadiusUv;
            sum += SampleLuminanceAtUv(Mathf.Clamp01(u + offset.x), Mathf.Clamp01(v + offset.y));
        }

        return sum / RegionSampleOffsets.Length;
    }

    private bool EvaluateAllowAtUv(float u, float v, float luminance, PaintRegionMode mode)
    {
        bool inFigure = IsInsideFigureSilhouette(u, v);

        switch (mode)
        {
            case PaintRegionMode.DarkBackgroundOnly:
                if (referencePixelsReady)
                    return luminance < figureFillLuminanceMin;

                return !inFigure;

            case PaintRegionMode.BottomBandOnly:
                return v <= 0.36f;

            case PaintRegionMode.FigureOnly:
                return inFigure;

            case PaintRegionMode.BrightAreasOnly:
                return inFigure && luminance >= brightAreaLuminanceMin;

            default:
                return true;
        }
    }

    private bool IsInsideFigureSilhouette(float u, float v)
    {
        float dx = (u - figureZoneCenterUv.x) / Mathf.Max(0.01f, figureZoneRadiusUv.x);
        float dy = (v - figureZoneCenterUv.y) / Mathf.Max(0.01f, figureZoneRadiusUv.y);
        return dx * dx + dy * dy <= 1f;
    }

    private float SampleLuminanceAtUv(float u, float v)
    {
        if (!referencePixelsReady)
            return 0.5f;

        int px = Mathf.Clamp(Mathf.RoundToInt(u * (cachedReferenceWidth - 1)), 0, cachedReferenceWidth - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(v * (cachedReferenceHeight - 1)), 0, cachedReferenceHeight - 1);
        Color pixel = cachedReferencePixels[py * cachedReferenceWidth + px];
        return pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f;
    }

    private bool TryGetSpriteUvFromInputPoint(Vector2 inputLocalPoint, out float u, out float v)
    {
        u = 0f;
        v = 0f;

        if (referenceArtwork == null || referenceArtwork.sprite == null)
            return false;

        if (inputArea == referenceArtwork.rectTransform)
            return TryGetSpriteUvFromImageLocal(inputLocalPoint, out u, out v);

        if (inputArea == null)
            return false;

        ResolveCamera();
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, inputArea.TransformPoint(inputLocalPoint));
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                referenceArtwork.rectTransform,
                screenPoint,
                uiCamera,
                out Vector2 imageLocal))
            return false;

        return TryGetSpriteUvFromImageLocal(imageLocal, out u, out v);
    }

    private bool TryGetArtworkDrawableLayout(
        out float drawWidth,
        out float drawHeight,
        out float offsetX,
        out float offsetY,
        out float rectWidth,
        out float rectHeight)
    {
        drawWidth = 0f;
        drawHeight = 0f;
        offsetX = 0f;
        offsetY = 0f;
        rectWidth = 0f;
        rectHeight = 0f;

        if (referenceArtwork == null || referenceArtwork.sprite == null)
            return false;

        Rect rect = referenceArtwork.rectTransform.rect;
        Sprite sprite = referenceArtwork.sprite;
        Rect spriteRect = sprite.rect;
        float spriteAspect = spriteRect.width / Mathf.Max(1f, spriteRect.height);
        float rectAspect = rect.width / Mathf.Max(1f, rect.height);
        rectWidth = rect.width;
        rectHeight = rect.height;

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

        return drawWidth > 0f && drawHeight > 0f;
    }

    private bool TryMapSpriteUvToOverlayUv(float spriteU, float spriteV, out float overlayU, out float overlayV)
    {
        overlayU = spriteU;
        overlayV = spriteV;

        if (!TryGetArtworkDrawableLayout(out float drawWidth, out float drawHeight, out float offsetX, out float offsetY, out float rectWidth, out float rectHeight))
            return referenceArtwork == null;

        overlayU = (offsetX + spriteU * drawWidth) / Mathf.Max(1f, rectWidth);
        overlayV = (offsetY + spriteV * drawHeight) / Mathf.Max(1f, rectHeight);
        return true;
    }

    private bool TryMapOverlayUvToSpriteUv(float overlayU, float overlayV, out float spriteU, out float spriteV)
    {
        spriteU = overlayU;
        spriteV = overlayV;

        if (!TryGetArtworkDrawableLayout(out float drawWidth, out float drawHeight, out float offsetX, out float offsetY, out float rectWidth, out float rectHeight))
            return referenceArtwork == null;

        float localX = overlayU * rectWidth - offsetX;
        float localY = overlayV * rectHeight - offsetY;
        if (localX < 0f || localY < 0f || localX > drawWidth || localY > drawHeight)
            return false;

        spriteU = localX / drawWidth;
        spriteV = localY / drawHeight;
        return true;
    }

    private bool TryGetSpriteUvFromImageLocal(Vector2 imageLocal, out float u, out float v)
    {
        u = 0f;
        v = 0f;

        if (referenceArtwork == null || referenceArtwork.sprite == null)
            return false;

        if (!TryGetArtworkDrawableLayout(out float drawWidth, out float drawHeight, out float offsetX, out float offsetY, out _, out _))
            return false;

        Rect rect = referenceArtwork.rectTransform.rect;
        float localX = imageLocal.x - rect.xMin - offsetX;
        float localY = imageLocal.y - rect.yMin - offsetY;
        if (localX < 0f || localY < 0f || localX > drawWidth || localY > drawHeight)
            return false;

        u = localX / drawWidth;
        v = localY / drawHeight;
        return true;
    }

    private static readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    private bool IsPointerOverBlockingUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);

        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            GameObject hit = uiRaycastResults[i].gameObject;
            if (hit != null && IsHudInteractive(hit.transform))
                return true;
        }

        return false;
    }

    // Блокируем рисование ТОЛЬКО над образцами палитры и кнопкой «Дальше».
    // НЕ над стрелками свайпа и фоном — иначе они перекрывают часть картины
    // и рисовать можно только там, где их нет.
    private bool IsHudInteractive(Transform hit)
    {
        if (paletteSwatches != null)
        {
            for (int s = 0; s < paletteSwatches.Length; s++)
            {
                Image swatch = paletteSwatches[s];
                if (swatch != null && (hit == swatch.transform || hit.IsChildOf(swatch.transform)))
                    return true;
            }
        }

        if (nextStageButton != null && (hit == nextStageButton.transform || hit.IsChildOf(nextStageButton.transform)))
            return true;

        return false;
    }

    private static bool TryGetActivePointer(out Vector2 screenPosition, out bool isHeld)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            screenPosition = touch.position;
            isHeld = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
            return true;
        }

        screenPosition = Input.mousePosition;
        isHeld = Input.GetMouseButton(0);
        return true;
    }

    private void ApplyStage(int stageIndex, bool force)
    {
        EnsureGameConfiguration();

        if (stages == null || stages.Length == 0)
        {
            UpdateProgressText();
            return;
        }

        if (!force)
            CommitCurrentStageProgress();

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
        nextLightSpawnDelay = UnityEngine.Random.Range(lightSpawnIntervalMin, lightSpawnIntervalMax);
        currentStageProgressCommitted = false;
        currentStageCompletedInTime = false;
        ClearLightTokens();
        wrongRegionAttempts = 0;
        regionHintActive = false;
        EnsureAllowedPaletteSelected();
        UpdateStageTexts();
        UpdateProgressText();
        UpdateTimerText();
        UpdatePaletteVisuals();
        SetNextStageButtonState(false);
        UpdateFeedbackForCurrentColor();
        BuildStageAllowMask();
        RebuildRegionHintTexture();
        UpdateRegionHintDisplay();
    }

    private void UpdateStageTexts()
    {
        StageDefinition stage = GetCurrentStage();

        if (stageTitleText != null)
            stageTitleText.text = string.IsNullOrWhiteSpace(stage.title)
                ? $"Этап {currentStageIndex + 1}"
                : stage.title;

        if (stageHintText != null)
        {
            if (!feedbackIsWarning)
                ApplyNormalText(stageHintText, stage.hint ?? string.Empty, defaultStageHintColor);
        }
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
            ShowBlockedPaintFeedback(WrongColorSelectionMessage);
    }

    private void ShowBlockedPaintFeedback(string message)
    {
        ShowWrongColorFeedback(message);
    }

    private void ShowBlockedPaintFeedbackThrottled(string message)
    {
        float cooldown = Mathf.Max(0.2f, blockFeedbackCooldownSeconds);
        if (message == lastBlockFeedbackMessage && Time.unscaledTime - lastBlockFeedbackTime < cooldown)
            return;

        lastBlockFeedbackTime = Time.unscaledTime;
        lastBlockFeedbackMessage = message;
        ShowBlockedPaintFeedback(message);
    }

    private void ShowWrongColorFeedback(string message)
    {
        feedbackIsWarning = true;

        if (feedbackText != null)
            ApplyWarningText(feedbackText, message);
    }

    private void SetFeedback(string message, bool isWarning = false)
    {
        if (isWarning)
        {
            ShowWrongColorFeedback(message);
            return;
        }

        feedbackIsWarning = false;

        if (feedbackText != null)
            ApplyNormalText(feedbackText, message, defaultFeedbackColor);

        RestoreStageHintText();
    }

    private void RestoreStageHintText()
    {
        if (stageHintText == null || GetCurrentStage() == null)
            return;

        ApplyNormalText(stageHintText, GetCurrentStage().hint ?? string.Empty, defaultStageHintColor);
    }

    private static void ApplyWarningText(TMP_Text text, string message)
    {
        if (text == null)
            return;

        text.richText = true;
        text.text = $"<color={WarningFeedbackHex}><b>{message}</b></color>";
        text.ForceMeshUpdate();
    }

    private static void ApplyNormalText(TMP_Text text, string message, Color color)
    {
        if (text == null)
            return;

        text.text = message;
        text.color = color;
        text.faceColor = color;
        text.ForceMeshUpdate();
    }

    private void EnsureGameConfiguration()
    {
        EnsureDefaultPalette();
        EnsureDefaultArtMovements();
        EnsureDefaultStages();
    }

    private ArtMovementDefinition GetCurrentMovement()
    {
        if (artMovements == null || artMovements.Length == 0)
            return null;

        int index = Mathf.Clamp(currentMovementIndex, 0, artMovements.Length - 1);
        return artMovements[index];
    }

    private bool HasNextMovement()
    {
        return artMovements != null && currentMovementIndex < artMovements.Length - 1;
    }

    private void EnsureDefaultArtMovements()
    {
        if (artMovements != null && artMovements.Length >= 2)
            return;

        artMovements = new[]
        {
            BuildImpressionismMovement(),
            BuildPointillismMovement()
        };
    }

    private void ResolveArtMovementSprites()
    {
        if (artMovements == null || artMovements.Length == 0)
            return;

        if (artMovements.Length > 0)
        {
            ArtMovementDefinition impressionism = artMovements[0];
            if (impressionism.referenceSprite == null)
                impressionism.referenceSprite = impressionismReferenceSprite != null
                    ? impressionismReferenceSprite
                    : referenceArtwork != null ? referenceArtwork.sprite : null;

            AssignExampleSprites(
                impressionism,
                impressionismExampleSprite1,
                impressionismExampleSprite2,
                impressionismExampleSprite3);

            if (impressionism.stampTexture == null)
                impressionism.stampTexture = impressionismStampTexture != null
                    ? impressionismStampTexture
                    : stampTexture;
        }

        if (artMovements.Length > 1)
        {
            ArtMovementDefinition pointillism = artMovements[1];
            if (pointillism.referenceSprite == null)
                pointillism.referenceSprite = pointillismReferenceSprite;

            AssignExampleSprites(
                pointillism,
                pointillismExampleSprite1,
                pointillismExampleSprite2,
                pointillismExampleSprite3);

            if (pointillism.stampTexture == null)
                pointillism.stampTexture = pointillismStampTexture;
        }
    }

    private static void AssignExampleSprites(
        ArtMovementDefinition movement,
        Sprite first,
        Sprite second,
        Sprite third)
    {
        if (movement == null)
            return;

        if (movement.exampleSprites == null || movement.exampleSprites.Length < 3)
            movement.exampleSprites = new Sprite[3];

        if (movement.exampleSprites[0] == null)
            movement.exampleSprites[0] = first;
        if (movement.exampleSprites[1] == null)
            movement.exampleSprites[1] = second;
        if (movement.exampleSprites[2] == null)
            movement.exampleSprites[2] = third;
    }

    private void ApplyCurrentMovement(bool clearPaint)
    {
        ArtMovementDefinition movement = GetCurrentMovement();
        if (movement == null)
            return;

        if (movement.paletteColors != null && movement.paletteColors.Length > 0)
            paletteColors = movement.paletteColors;

        if (movement.stages != null && movement.stages.Length > 0)
            stages = movement.stages;

        stampTexture = movement.stampTexture;
        useDotStamps = movement.useDotStamps;
        fallbackStampSize = movement.fallbackStampSize;
        dragSpacingPixels = movement.dragSpacingPixels;
        baseStampScale = movement.baseStampScale;
        scaleJitter = movement.scaleJitter;
        rotationJitterDegrees = movement.rotationJitterDegrees;
        opacityJitter = movement.opacityJitter;
        strokeOverlapBlend = movement.strokeOverlapBlend;
        strokeColorJitter = movement.strokeColorJitter;
        figureZoneCenterUv = movement.figureZoneCenterUv;
        figureZoneRadiusUv = movement.figureZoneRadiusUv;

        strokePainter.InvalidateStampCache();

        if (referenceArtwork != null && movement.referenceSprite != null)
            referenceArtwork.sprite = movement.referenceSprite;

        referencePixelsReady = false;
        cachedReferencePixels = null;
        EnsureStrokePainter();

        if (paletteSwatches != null && paletteColors != null)
        {
            for (int i = 0; i < paletteSwatches.Length && i < paletteColors.Length; i++)
            {
                if (paletteSwatches[i] != null)
                    paletteSwatches[i].color = paletteColors[i];
            }
        }

        currentPaletteIndex = Mathf.Clamp(defaultPaletteIndex, 0, Mathf.Max(0, paletteColors.Length - 1));
        UpdatePaletteVisuals();

        if (clearPaint)
            ClearCanvas();
    }

    private static ArtMovementDefinition BuildImpressionismMovement()
    {
        return new ArtMovementDefinition
        {
            id = "impressionism",
            resultTitle = "Импрессионизм",
            useDotStamps = false,
            fallbackStampSize = new Vector2(72f, 28f),
            dragSpacingPixels = 22f,
            baseStampScale = 0.1f,
            scaleJitter = 0.1f,
            rotationJitterDegrees = 35f,
            opacityJitter = 0.06f,
            strokeOverlapBlend = 0.48f,
            strokeColorJitter = 0f, // рисуем строго выбранным цветом, без сдвига оттенка
            figureZoneCenterUv = new Vector2(0.5f, 0.5f),
            figureZoneRadiusUv = new Vector2(0.24f, 0.34f),
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
            },
            stages = BuildImpressionismStages(),
            description =
                "Импрессионизм — направление в живописи второй половины XIX века. " +
                "Художники стремились передать мгновенное впечатление от увиденного: свет, воздух и движение.\n\n" +
                "Главные признаки — короткие отдельные мазки и яркие чистые цвета, которые глаз «смешивает» на расстоянии. " +
                "Часто картины писали на воздухе, прямо перед натурой.\n\n" +
                "В этой игре ты попробовала именно это: короткие штрихи, живую палитру и работу цветом вместо ровной заливки."
        };
    }

    private static ArtMovementDefinition BuildPointillismMovement()
    {
        return new ArtMovementDefinition
        {
            id = "pointillism",
            resultTitle = "Пуантилизм",
            useDotStamps = true,
            fallbackStampSize = new Vector2(24f, 24f),
            dragSpacingPixels = 10f,
            baseStampScale = 0.055f,
            scaleJitter = 0.04f,
            rotationJitterDegrees = 4f,
            opacityJitter = 0.04f,
            strokeOverlapBlend = 0.52f,
            strokeColorJitter = 0f, // рисуем строго выбранным цветом, без сдвига оттенка
            figureZoneCenterUv = new Vector2(0.5f, 0.48f),
            figureZoneRadiusUv = new Vector2(0.34f, 0.4f),
            paletteColors = new[]
            {
                Hex("3D7CB8"),
                Hex("A8B8C8"),
                Hex("142D4A"),
                Hex("7A3528"),
                Hex("9E4A32"),
                Hex("4A9A48"),
                Hex("2D6E30"),
                Hex("D42828"),
                Hex("F0EDE8"),
                Hex("E8C820"),
                Hex("9E5A98"),
                Hex("D8A8D0"),
                Hex("1A1A1A"),
                Hex("2060A8"),
                Hex("F8A8C0")
            },
            stages = BuildPointillismStages(),
            description =
                "Пуантилизм — направление в живописи конца XIX века. " +
                "Художники рисовали не мазками, а отдельными точками чистого цвета.\n\n" +
                "Главная идея — оптическое смешение: рядом лежат синие и жёлтые точки, а глаз видит зелёный. " +
                "Картина собирается из множества маленьких пятен, как мозаика света.\n\n" +
                "В этой игре ты попробовала пуантилизм: ставила плотные цветные точки и работала чистыми оттенками, " +
                "характерными для этой картины."
        };
    }

    private static StageDefinition[] BuildImpressionismStages()
    {
        return BuildDefaultStages();
    }

    private static StageDefinition[] BuildPointillismStages()
    {
        return new[]
        {
            new StageDefinition
            {
                title = "Потолок",
                hint = "Начни с полосатого потолка. Положи голубые и серые точки.",
                requiredStrokeCount = 12,
                allowedPaletteIndices = new[] { 0, 1 },
                paintRegionMode = PaintRegionMode.Anywhere
            },
            new StageDefinition
            {
                title = "Деревянный пол",
                hint = "Теперь собери красно-коричневый пол из точек.",
                requiredStrokeCount = 10,
                allowedPaletteIndices = new[] { 3, 4 },
                paintRegionMode = PaintRegionMode.Anywhere
            },
            new StageDefinition
            {
                title = "Синие стены",
                hint = "Добавь тёмно-синие и насыщенные синие массы на стенах.",
                requiredStrokeCount = 12,
                allowedPaletteIndices = new[] { 2, 13 },
                paintRegionMode = PaintRegionMode.Anywhere
            },
            new StageDefinition
            {
                title = "Зелёный луг",
                hint = "За окном — яркая зелень. Используй светлые и тёмные зелёные точки.",
                requiredStrokeCount = 10,
                allowedPaletteIndices = new[] { 5, 6 },
                paintRegionMode = PaintRegionMode.Anywhere
            },
            new StageDefinition
            {
                title = "Красные акценты",
                hint = "Клоун, попугай и яркие детали — красные точки.",
                requiredStrokeCount = 8,
                allowedPaletteIndices = new[] { 7 },
                paintRegionMode = PaintRegionMode.Anywhere
            },
            new StageDefinition
            {
                title = "Белые формы",
                hint = "Пуанты, пудель и светлые ткани — белые и кремовые точки.",
                requiredStrokeCount = 10,
                allowedPaletteIndices = new[] { 8 },
                paintRegionMode = PaintRegionMode.Anywhere
            },
            new StageDefinition
            {
                title = "Сирень",
                hint = "Собери букет сирени из розовых и лиловых точек.",
                requiredStrokeCount = 8,
                allowedPaletteIndices = new[] { 10, 11, 14 },
                paintRegionMode = PaintRegionMode.Anywhere
            },
            new StageDefinition
            {
                title = "Финальные детали",
                hint = "Добавь жёлтый, чёрный и синие акценты в деталях сцены.",
                requiredStrokeCount = 8,
                allowedPaletteIndices = new[] { 9, 12, 13 },
                paintRegionMode = PaintRegionMode.Anywhere
            }
        };
    }

    private bool HasValidStageConfiguration()
    {
        if (stages == null || stages.Length == 0)
            return false;

        for (int i = 0; i < stages.Length; i++)
        {
            StageDefinition stage = stages[i];
            if (stage == null || stage.allowedPaletteIndices == null || stage.allowedPaletteIndices.Length == 0)
                return false;
        }

        return true;
    }

    private void EnsureAllowedPaletteSelected()
    {
        StageDefinition stage = GetCurrentStage();
        if (stage == null || stage.allowedPaletteIndices == null || stage.allowedPaletteIndices.Length == 0)
            return;

        if (IsCurrentColorAllowed())
            return;

        currentPaletteIndex = stage.allowedPaletteIndices[0];
    }

    private void UpdateBonusScoreText()
    {
        if (bonusScoreText != null)
            bonusScoreText.text = $"Солнышки: {lightsCollectedThisStage}/{totalLightsRequired}";
    }

    private void UpdateTimers()
    {
        if (GetCurrentStage() == null || stageExpired)
            return;

        stageTimeRemaining = Mathf.Max(0f, stageTimeRemaining - Time.deltaTime);
        idleTime += Time.deltaTime;
        UpdateTimerText();

        if (!idleHintShown && !feedbackIsWarning && idleTime >= Mathf.Max(0.5f, idleHintDelaySeconds))
        {
            idleHintShown = true;
            SetFeedback(idleHintMessage);
        }

        if (stageTimeRemaining <= 0f)
        {
            stageExpired = true;
            if (IsLastStage())
            {
                SetFeedback("Время закончилось, но работа зафиксирована. Подводим итог.");
                CompleteSession();
            }
            else
            {
                SetFeedback("Время этапа закончилось. Можно перейти дальше или попробовать еще раз.");
                SetNextStageButtonState(true);
            }
        }
    }

    private void UpdateLightTokens()
    {
        if (inputArea == null || stageExpired)
            return;

        if (totalLightsSpawnedThisSession < totalLightsRequired)
        {
            nextLightSpawnDelay -= Time.deltaTime;
            if (nextLightSpawnDelay <= 0f)
            {
                SpawnLightToken();
                nextLightSpawnDelay = UnityEngine.Random.Range(lightSpawnIntervalMin, lightSpawnIntervalMax);
            }
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
        if (debugOverlayRoot == null || canvasRect == null)
            return;

        RectTransform spawnRect = GetArtworkInputRect();
        if (spawnRect == null)
            return;

        float paddingX = Mathf.Min(50f, Mathf.Abs(spawnRect.rect.width) * 0.2f);
        float paddingY = Mathf.Min(50f, Mathf.Abs(spawnRect.rect.height) * 0.2f);
        Vector2 localPoint = new Vector2(
            UnityEngine.Random.Range(spawnRect.rect.xMin + paddingX, spawnRect.rect.xMax - paddingX),
            UnityEngine.Random.Range(spawnRect.rect.yMin + paddingY, spawnRect.rect.yMax - paddingY));

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
        totalLightsSpawnedThisSession++;
    }

    private bool TryCatchLight(Vector2 strokeLocalPoint)
    {
        for (int i = activeLights.Count - 1; i >= 0; i--)
        {
            LightToken token = activeLights[i];
            if (Vector2.Distance(token.localPoint, strokeLocalPoint) > lightCatchRadiusPixels)
                continue;

            lightsCollectedThisStage++;
            lightsBonusEarned = lightsCollectedThisStage >= totalLightsRequired;
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

        bool isLastStage = IsLastStage();
        nextStageButton.interactable = isReady && !isLastStage && !sessionFinished;
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

    private bool IsLastStage()
    {
        return stages == null || currentStageIndex >= stages.Length - 1;
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
            baseColor.a = Mathf.Clamp01(opacity);
        }

        return baseColor;
    }

    private Color ApplyStrokeColorJitter(Color baseColor)
    {
        if (strokeColorJitter <= 0f)
            return baseColor;

        Color.RGBToHSV(baseColor, out float h, out float s, out float v);
        h = Mathf.Repeat(h + UnityEngine.Random.Range(-strokeColorJitter, strokeColorJitter), 1f);
        s = Mathf.Clamp01(s + UnityEngine.Random.Range(-strokeColorJitter * 0.5f, strokeColorJitter * 0.5f));
        v = Mathf.Clamp01(v + UnityEngine.Random.Range(-strokeColorJitter * 0.35f, strokeColorJitter * 0.35f));
        Color jittered = Color.HSVToRGB(h, s, v);
        jittered.a = baseColor.a;
        return jittered;
    }

    private void ResolveCamera()
    {
        if (targetCanvas == null && referenceArtwork != null)
            targetCanvas = referenceArtwork.canvas;

        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        if (targetCanvas == null)
        {
            uiCamera = null;
            canvasRect = null;
            return;
        }

        canvasRect = targetCanvas.transform as RectTransform;
        uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : targetCanvas.worldCamera != null ? targetCanvas.worldCamera : Camera.main;
    }

    private Camera GetCameraForRectTransform(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return uiCamera;

        Canvas parentCanvas = rectTransform.GetComponentInParent<Canvas>();
        if (parentCanvas == null)
            return uiCamera;

        return parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main;
    }

    private RectTransform GetArtworkInputRect()
    {
        if (referenceArtwork != null)
            return referenceArtwork.rectTransform;

        return inputArea;
    }

    private bool TryGetLocalPoint(Vector2 screenPosition, out Vector2 localPoint)
    {
        return strokePainter.TryScreenToStrokeUv(screenPosition, out _, out _, out localPoint);
    }

    private bool TryGetArtworkUvFromPaintSurfaceUv(float paintU, float paintV, out float artworkU, out float artworkV)
    {
        artworkU = paintU;
        artworkV = paintV;

        if (inputArea == null || referenceArtwork == null)
            return referenceArtwork == null;

        Vector2 surfaceLocal = new Vector2(
            Mathf.Lerp(inputArea.rect.xMin, inputArea.rect.xMax, paintU),
            Mathf.Lerp(inputArea.rect.yMin, inputArea.rect.yMax, paintV));

        if (TryGetSpriteUvFromImageLocal(surfaceLocal, out artworkU, out artworkV))
            return true;

        artworkU = paintU;
        artworkV = paintV;
        return true;
    }

    private bool TryGetArtworkLocalFromUv(float u, float v, out Vector2 imageLocal)
    {
        imageLocal = default;

        if (referenceArtwork == null || referenceArtwork.sprite == null)
            return false;

        Rect rect = referenceArtwork.rectTransform.rect;
        Sprite sprite = referenceArtwork.sprite;
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

        imageLocal = new Vector2(
            rect.xMin + offsetX + u * drawWidth,
            rect.yMin + offsetY + v * drawHeight);
        return true;
    }

    private bool TryConvertInputLocalToCanvasLocal(Vector2 inputLocalPoint, out Vector2 canvasLocalPoint)
    {
        RectTransform paintRect = GetArtworkInputRect();
        if (paintRect == null || canvasRect == null)
        {
            canvasLocalPoint = default;
            return false;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, paintRect.TransformPoint(inputLocalPoint));
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

    private void CreateDebugMarker(Vector2 localPoint)
    {
        EnsureDebugOverlayRoot();
        if (!showDebugMarkers || debugOverlayRoot == null || canvasRect == null)
            return;

        RectTransform paintRect = GetArtworkInputRect();
        if (paintRect == null)
            return;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, paintRect.TransformPoint(localPoint));
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

    private void CommitCurrentStageProgress()
    {
        if (currentStageProgressCommitted || GetCurrentStage() == null)
            return;

        currentStageProgressCommitted = true;

        if (currentStageStrokeCount >= GetCurrentStage().requiredStrokeCount && currentStageCompletedInTime)
            completedStagesInTimeCount++;
    }

    private int GetTotalRequiredStrokes()
    {
        if (stages == null || stages.Length == 0)
            return 1;

        int total = 0;
        for (int i = 0; i < stages.Length; i++)
            total += Mathf.Max(0, stages[i].requiredStrokeCount);

        return Mathf.Max(1, total);
    }

    private int GetTotalRequiredColors()
    {
        if (stages == null || stages.Length == 0)
            return 1;

        HashSet<int> requiredColors = new HashSet<int>();
        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i].allowedPaletteIndices == null)
                continue;

            for (int j = 0; j < stages[i].allowedPaletteIndices.Length; j++)
                requiredColors.Add(stages[i].allowedPaletteIndices[j]);
        }

        return Mathf.Max(1, requiredColors.Count);
    }

    private float CalculateSessionScore()
    {
        float strokeRatio = Mathf.Clamp01((float)totalRegisteredStrokes / GetTotalRequiredStrokes());
        float colorRatio = Mathf.Clamp01((float)usedPaletteIndices.Count / GetTotalRequiredColors());
        float timeRatio = stages == null || stages.Length == 0
            ? 0f
            : Mathf.Clamp01((float)completedStagesInTimeCount / stages.Length);

        float baseScore = (strokeRatio * 9f) + (colorRatio * 3f) + (timeRatio * 3f);
        float totalScore = baseScore + (lightsBonusEarned ? 1f : 0f);
        return (float)Math.Round(totalScore, 1, MidpointRounding.AwayFromZero);
    }

    private void CompleteSession()
    {
        if (sessionFinished)
            return;

        CommitCurrentStageProgress();
        sessionFinished = true;
        stageExpired = true;
        currentAttemptPoints = CalculateSessionScore();
        SaveCompletedSession(currentAttemptPoints);
        UpdateMiniGameStatusText();
        ClearLightTokens();

        string formattedScore = FormatScore(currentAttemptPoints);
        if (lightsBonusEarned)
            SetFeedback($"Работа завершена. Вы набрали {formattedScore}/16 очков и собрали 5/5 солнышек.");
        else
            SetFeedback($"Работа завершена. Вы набрали {formattedScore}/16 очков. За солнышки бонус не начислен.");

        if (nextStageButton != null)
            nextStageButton.interactable = false;

        ShowFinalScreen(formattedScore);
    }

    private void SaveCompletedSession(float points)
    {
        lastCompletedPoints = points;
        hasSavedCompletedSession = true;
        PlayerPrefs.SetFloat(SavedArtworkPointsKey, points);
        PlayerPrefs.SetInt(SavedArtworkCompletedKey, 1);
        PlayerPrefs.Save();
        LocalProfileProgression.NotifyChanged();
    }

    private void LoadSavedSession()
    {
        lastCompletedPoints = PlayerPrefs.GetFloat(SavedArtworkPointsKey, 0f);
        hasSavedCompletedSession = PlayerPrefs.GetInt(SavedArtworkCompletedKey, 0) == 1;
    }

    private void ClearSavedSession()
    {
        lastCompletedPoints = 0f;
        hasSavedCompletedSession = false;
        PlayerPrefs.DeleteKey(SavedArtworkPointsKey);
        PlayerPrefs.DeleteKey(SavedArtworkCompletedKey);
        PlayerPrefs.Save();
        LocalProfileProgression.NotifyChanged();
    }

    private string FormatScore(float value)
    {
        return value.ToString("0.#", CultureInfo.GetCultureInfo("ru-RU"));
    }

    private RectTransform FindMiniGameCard()
    {
        GameObject cardObject = GameObject.Find(MiniGameCardObjectName);
        return cardObject != null ? cardObject.GetComponent<RectTransform>() : null;
    }

    private void EnsureMiniGameStatusText()
    {
        if (miniGameStatusText != null)
            return;

        if (miniGameCard == null)
            miniGameCard = FindMiniGameCard();

        if (miniGameCard == null)
            return;

        TMP_Text[] texts = miniGameCard.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].gameObject.name == "PaintStatusText")
            {
                miniGameStatusText = texts[i];
                return;
            }
        }
    }

    private void UpdateMiniGameStatusText()
    {
        if (miniGameStatusText == null)
            return;

        if (!hasSavedCompletedSession)
        {
            miniGameStatusText.text = "Сохраненного результата нет";
            return;
        }

        miniGameStatusText.text = $"Последняя попытка: {FormatScore(lastCompletedPoints)}/16";
    }

    private void EnsureScreenReferences()
    {
        if (miniGameScreen == null)
        {
            GameObject screen = GameObject.Find(MiniGameScreenObjectName);
            if (screen != null)
                miniGameScreen = screen;
        }

        if (miniGamesScreen == null)
        {
            GameObject screen = GameObject.Find(MiniGamesScreenObjectName);
            if (screen != null)
                miniGamesScreen = screen;
        }

        if (prologueScreen == null)
        {
            GameObject screen = GameObject.Find(PrologueScreenObjectName);
            if (screen != null)
                prologueScreen = screen;
        }

        if (finalScreen == null)
        {
            GameObject screen = GameObject.Find(FinalScreenObjectName);
            if (screen != null)
                finalScreen = screen;
        }

        if (homeScreen == null)
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < allObjects.Length; i++)
            {
                GameObject candidate = allObjects[i];
                if (candidate == null || candidate.name != HomeScreenObjectName)
                    continue;

                if (candidate.scene.IsValid() && candidate.transform.parent == transform.parent)
                {
                    homeScreen = candidate;
                    break;
                }
            }
        }
    }

    private void EnsureFinalResultText()
    {
        if (finalResultText != null)
            return;

        EnsureScreenReferences();
        if (finalScreen == null)
            return;

        TMP_Text[] texts = finalScreen.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].text.Contains("Результат пользователя"))
            {
                finalResultText = texts[i];
                return;
            }
        }
    }

    private void EnsureFinalScreenContentReferences()
    {
        EnsureScreenReferences();
        if (finalScreen == null)
            return;

        if (movementContentRoot == null)
        {
            Transform impressionism = finalScreen.transform.Find("Impressionism");
            if (impressionism != null)
                movementContentRoot = impressionism.gameObject;
        }

        if (movementDescriptionText == null)
        {
            Transform description = finalScreen.transform.Find("Description");
            if (description != null)
                movementDescriptionText = description.GetComponent<TMP_Text>();
        }

        if (movementDescriptionText == null)
        {
            TMP_Text[] texts = finalScreen.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || text == finalResultText)
                    continue;

                if (text.gameObject.name == "Description")
                {
                    movementDescriptionText = text;
                    break;
                }
            }
        }

        EnsureFinalScreenActionButtonReferences();
        EnsureMovementExampleImages();
    }

    private void EnsureFinalScreenActionButtonReferences()
    {
        if (finalScreenActionButton != null && finalScreenActionButtonText != null)
            return;

        if (finalScreen == null)
            return;

        Transform again = finalScreen.transform.Find("Again");
        if (again == null)
            return;

        if (finalScreenActionButton == null)
            finalScreenActionButton = again.GetComponent<Button>();

        if (finalScreenActionButtonText == null)
            finalScreenActionButtonText = again.GetComponentInChildren<TMP_Text>(true);
    }

    private void EnsureMovementExampleImages()
    {
        if (movementExampleImages != null && movementExampleImages.Length > 0)
            return;

        if (movementContentRoot == null)
            return;

        movementExampleImages = movementContentRoot.GetComponentsInChildren<Image>(true);
    }

    private void ApplyFinalScreenContent(string formattedScore)
    {
        EnsureFinalScreenContentReferences();
        ArtMovementDefinition movement = GetCurrentMovement();
        string movementTitle = movement != null && !string.IsNullOrWhiteSpace(movement.resultTitle)
            ? movement.resultTitle
            : "Результат";

        if (finalResultText != null)
            finalResultText.text = $"{movementTitle}: {formattedScore}/16 очков";

        if (movementDescriptionText != null)
            movementDescriptionText.text = movement != null ? movement.description ?? string.Empty : string.Empty;

        if (movementContentRoot != null)
            movementContentRoot.SetActive(true);

        if (movementDescriptionText != null)
            movementDescriptionText.gameObject.SetActive(true);

        if (movement != null && movement.exampleSprites != null && movementExampleImages != null)
        {
            int imageCount = Mathf.Min(movement.exampleSprites.Length, movementExampleImages.Length);
            for (int i = 0; i < imageCount; i++)
            {
                if (movementExampleImages[i] != null && movement.exampleSprites[i] != null)
                {
                    movementExampleImages[i].sprite = movement.exampleSprites[i];
                    // Заполняем рамку целиком, чтобы верх/низ всех трёх совпадали.
                    movementExampleImages[i].preserveAspect = false;
                }
            }
        }

        UpdateFinalScreenActionButton();
    }

    private void UpdateFinalScreenActionButton()
    {
        EnsureFinalScreenActionButtonReferences();

        bool hasNext = HasNextMovement();
        if (finalScreenActionButtonText != null)
            finalScreenActionButtonText.text = hasNext ? "Дальше" : "Заново";
    }

    private void ShowFinalScreen(string formattedScore)
    {
        EnsureScreenReferences();
        EnsureFinalResultText();
        ApplyFinalScreenContent(formattedScore);

        isShowingFinalScreen = true;

        if (miniGameScreen != null)
            miniGameScreen.SetActive(false);

        if (finalScreen != null)
            finalScreen.SetActive(true);

        strokePainter.SetVisible(false);
    }

    private void HideFinalScreen()
    {
        EnsureScreenReferences();
        isShowingFinalScreen = false;

        if (finalScreen != null)
            finalScreen.SetActive(false);
    }

    private void ShowMiniGameScreen()
    {
        EnsureScreenReferences();

        if (miniGamesScreen != null)
            miniGamesScreen.SetActive(false);

        if (miniGameScreen != null)
            miniGameScreen.SetActive(true);

        EnsurePlayableViewVisible();
    }

    private void EnsurePlayableViewVisible()
    {
        EnsureScreenReferences();

        SwipeController swipeController = null;
        if (miniGameScreen != null)
            swipeController = miniGameScreen.GetComponentInChildren<SwipeController>(true);

        if (swipeController == null)
            swipeController = FindObjectOfType<SwipeController>();

        if (swipeController != null)
            swipeController.SnapToPlayable();
    }

    private void ShowMiniGamesScreen()
    {
        EnsureScreenReferences();

        if (miniGameScreen != null)
            miniGameScreen.SetActive(false);

        if (homeScreen != null)
            homeScreen.SetActive(false);

        if (miniGamesScreen != null)
            miniGamesScreen.SetActive(true);
    }

    private void ResetSessionState()
    {
        ClearCanvas();
        lightsCollectedThisStage = 0;
        lightRayActive = false;
        totalRegisteredStrokes = 0;
        completedStagesInTimeCount = 0;
        currentStageProgressCommitted = false;
        currentStageCompletedInTime = false;
        totalLightsSpawnedThisSession = 0;
        currentAttemptPoints = 0f;
        sessionFinished = false;
        lightsBonusEarned = false;
        isShowingFinalScreen = false;
        usedPaletteIndices.Clear();
        UpdateBonusScoreText();
        EnsureFinalResultText();
        if (finalResultText != null)
            finalResultText.text = "Результат пользователя";
        ApplyStage(0, true);
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
        if (HasValidStageConfiguration())
            return;

        stages = BuildDefaultStages();
    }

    private static StageDefinition[] BuildDefaultStages()
    {
        return new[]
        {
            new StageDefinition
            {
                title = "Фон",
                hint = "Сначала закрась фон вокруг фигуры. Не закрашивай клоуна и петуха.",
                requiredStrokeCount = 12,
                allowedPaletteIndices = new[] { 0, 1, 2 },
                paintRegionMode = PaintRegionMode.Anywhere
            },
            new StageDefinition
            {
                title = "Красная сцена",
                hint = "Теперь положи яркую нижнюю плоскость.",
                requiredStrokeCount = 10,
                allowedPaletteIndices = new[] { 5, 6 },
                paintRegionMode = PaintRegionMode.BottomBandOnly
            },
            new StageDefinition
            {
                title = "Пиджак и темные массы",
                hint = "Собери крупные темные формы фигуры.",
                requiredStrokeCount = 14,
                allowedPaletteIndices = new[] { 3, 4 },
                paintRegionMode = PaintRegionMode.FigureOnly
            },
            new StageDefinition
            {
                title = "Красные акценты",
                hint = "Добавь ритм красными акцентами.",
                requiredStrokeCount = 8,
                allowedPaletteIndices = new[] { 5, 6 },
                paintRegionMode = PaintRegionMode.FigureOnly
            },
            new StageDefinition
            {
                title = "Лицо и руки",
                hint = "Теперь теплые телесные оттенки.",
                requiredStrokeCount = 8,
                allowedPaletteIndices = new[] { 10, 11 },
                paintRegionMode = PaintRegionMode.BrightAreasOnly
            },
            new StageDefinition
            {
                title = "Волосы и теплые детали",
                hint = "Добавь теплые рыжие и охристые места.",
                requiredStrokeCount = 6,
                allowedPaletteIndices = new[] { 12, 13 },
                paintRegionMode = PaintRegionMode.FigureOnly
            },
            new StageDefinition
            {
                title = "Петух",
                hint = "Собери светлую массу петуха и не забудь про холодные тени.",
                requiredStrokeCount = 16,
                allowedPaletteIndices = new[] { 7, 8, 9 },
                paintRegionMode = PaintRegionMode.BrightAreasOnly
            },
            new StageDefinition
            {
                title = "Финальные детали",
                hint = "Проверь шляпу, рубашку, обувь и мелкие контрасты.",
                requiredStrokeCount = 8,
                allowedPaletteIndices = new[] { 3, 7, 13, 14 },
                paintRegionMode = PaintRegionMode.Anywhere
            }
        };
    }

    private void EnsureHudReferences()
    {
        if (miniGameScreen == null)
            EnsureScreenReferences();

        if (miniGameScreen == null)
            return;

        TMP_Text[] texts = miniGameScreen.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
                continue;

            if (feedbackText == null && text.gameObject.name == "Feedback Text")
                feedbackText = text;

            if (stageHintText == null && text.gameObject.name == "Stage Hint Text")
                stageHintText = text;
        }

        if (feedbackText != null)
            defaultFeedbackColor = feedbackText.color;

        if (stageHintText != null)
            defaultStageHintColor = stageHintText.color;
    }

    private void EnsureReferenceArtwork()
    {
        if (referenceArtwork == null)
        {
            if (miniGameScreen == null)
                EnsureScreenReferences();

            if (miniGameScreen == null)
                return;

            Image[] images = miniGameScreen.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image != null && image.gameObject.name == "Image_Playable")
                {
                    referenceArtwork = image;
                    break;
                }
            }
        }

        if (GetCurrentStage() == null)
        {
            EnsureGameConfiguration();
            ApplyCurrentMovement(false);
            if (stages != null && stages.Length > 0)
                ApplyStage(0, true);
        }

        CacheReferenceArtworkPixels();
        EnsureStrokePainter();
    }

    private void EnsureHudDoesNotBlockPaint()
    {
        if (miniGameScreen == null)
            EnsureScreenReferences();

        if (miniGameScreen == null)
            return;

        Transform paletteRoot = miniGameScreen.transform.Find("Palette Swatches");
        if (paletteRoot != null)
        {
            Image paletteBackground = paletteRoot.GetComponent<Image>();
            if (paletteBackground != null)
                paletteBackground.raycastTarget = false;
        }

        TMP_Text[] texts = miniGameScreen.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
                continue;

            if (text.gameObject.name == "Feedback Text"
                || text.gameObject.name == "Stage Hint Text"
                || text.gameObject.name == "Stroke Progress Text"
                || text.gameObject.name == "Timer Text")
            {
                text.raycastTarget = false;
            }
        }
    }

    private void BuildStageAllowMask()
    {
        stageAllowMaskReady = false;
        stageAllowMask = null;

        StageDefinition stage = GetCurrentStage();
        if (stage == null || stage.paintRegionMode == PaintRegionMode.Anywhere)
            return;

        stageMaskWidth = StageMaskResolution;
        stageMaskHeight = StageMaskResolution;
        int pixelCount = stageMaskWidth * stageMaskHeight;
        stageAllowMask = new bool[pixelCount];
        PaintRegionMode mode = stage.paintRegionMode;

        for (int y = 0; y < stageMaskHeight; y++)
        {
            for (int x = 0; x < stageMaskWidth; x++)
            {
                float u = x / (float)Mathf.Max(1, stageMaskWidth - 1);
                float v = y / (float)Mathf.Max(1, stageMaskHeight - 1);
                float luminance = SampleLuminanceAtUv(u, v);
                stageAllowMask[y * stageMaskWidth + x] = EvaluateAllowAtUv(u, v, luminance, mode);
            }
        }

        stageAllowMaskReady = true;
    }

    private float GetLuminanceAtPixel(int x, int y)
    {
        if (!referencePixelsReady)
            return 0.5f;

        Color pixel = cachedReferencePixels[y * cachedReferenceWidth + x];
        return pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f;
    }

    private void CacheReferenceArtworkPixels()
    {
        referencePixelsReady = false;
        cachedReferencePixels = null;
        cachedReferenceWidth = 0;
        cachedReferenceHeight = 0;

        if (referenceArtwork == null || referenceArtwork.sprite == null)
            return;

        Sprite sprite = referenceArtwork.sprite;
        Texture2D texture = sprite.texture;
        if (texture == null)
            return;

        Rect spriteRect = sprite.textureRect;
        int width = Mathf.Max(1, (int)spriteRect.width);
        int height = Mathf.Max(1, (int)spriteRect.height);

        try
        {
            cachedReferencePixels = texture.GetPixels((int)spriteRect.x, (int)spriteRect.y, width, height);
            cachedReferenceWidth = width;
            cachedReferenceHeight = height;
            referencePixelsReady = cachedReferencePixels != null && cachedReferencePixels.Length == width * height;
        }
        catch (Exception ex)
        {
            // Texture2D.GetPixels на не-readable текстуре бросает ArgumentException,
            // а не UnityException — ловим любой тип, иначе исключение всплывает из
            // Awake/OnEnable и рвёт всю инициализацию мини-игры.
            Debug.LogWarning($"LiveContourMiniGame: не удалось прочитать пиксели иллюстрации ({ex.Message}). Используется геометрическая проверка зоны.");
        }
        finally
        {
            BuildStageAllowMask();
        }
    }

    private void RegisterWrongRegionAttempt()
    {
        wrongRegionAttempts++;
    }

    private void EnsureRegionHintLayer()
    {
        if (regionHintLayer != null && regionHintTexture != null)
            return;

        EnsureStrokePainter();
        RectTransform overlayRoot = strokePainter.OverlayRoot;
        if (overlayRoot == null)
            return;

        Transform existing = overlayRoot.Find("RegionHintLayer");
        if (existing != null)
            regionHintLayer = existing.GetComponent<RawImage>();

        if (regionHintLayer == null)
        {
            GameObject layerObject = new GameObject("RegionHintLayer", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            RectTransform layerRect = layerObject.GetComponent<RectTransform>();
            layerRect.SetParent(overlayRoot, false);
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            regionHintLayer = layerObject.GetComponent<RawImage>();
        }

        regionHintLayer.raycastTarget = false;

        Transform canvasBacking = overlayRoot.Find("CanvasBacking");
        int hintIndex = canvasBacking != null ? canvasBacking.GetSiblingIndex() + 1 : 0;
        regionHintLayer.rectTransform.SetSiblingIndex(hintIndex);

        if (regionHintTexture == null)
        {
            int width = Mathf.Max(128, canvasSize.x);
            int height = Mathf.Max(128, canvasSize.y);
            regionHintTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            regionHintTexture.wrapMode = TextureWrapMode.Clamp;
            regionHintTexture.filterMode = FilterMode.Bilinear;
            regionHintLayer.texture = regionHintTexture;
        }
    }

    private void RebuildRegionHintTexture()
    {
        EnsureRegionHintLayer();
        if (regionHintTexture == null || inputArea == null)
            return;

        StageDefinition stage = GetCurrentStage();
        if (stage == null || stage.paintRegionMode == PaintRegionMode.Anywhere)
        {
            if (regionHintLayer != null)
                regionHintLayer.gameObject.SetActive(false);
            return;
        }

        int width = regionHintTexture.width;
        int height = regionHintTexture.height;
        Color[] pixels = new Color[width * height];
        Color hint = regionHintColor;
        PaintRegionMode mode = stage.paintRegionMode;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)Mathf.Max(1, width - 1);
                float v = y / (float)Mathf.Max(1, height - 1);
                float checkU = u;
                float checkV = v;
                if (referenceArtwork != null)
                    TryGetArtworkUvFromPaintSurfaceUv(u, v, out checkU, out checkV);

                bool allowed = IsAllowedAtUv(checkU, checkV, mode);
                pixels[y * width + x] = allowed
                    ? new Color(hint.r, hint.g, hint.b, hint.a)
                    : Color.clear;
            }
        }

        regionHintTexture.SetPixels(pixels);
        regionHintTexture.Apply(false);
    }

    private void UpdateRegionHintDisplay()
    {
        if (regionHintLayer == null)
            return;

        regionHintLayer.gameObject.SetActive(false);
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

