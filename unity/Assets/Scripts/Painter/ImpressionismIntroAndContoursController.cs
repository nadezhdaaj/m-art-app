
using TMPro;
using UnityEngine;

public class ImpressionismIntroAndContoursController : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject parisIntroPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject resultPanel;

    [Header("References")]
    [SerializeField] private Painter painter;

    [Header("HUD")]
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text instructionText;

    [Header("Result")]
    [SerializeField] private TMP_Text contoursScoreText;
    [SerializeField] private TMP_Text totalScoreText;
    [SerializeField] private TMP_Text badgeText;
    [SerializeField] private TMP_Text resultCommentText;

    [Header("Tuning")]
    [SerializeField] private float contourPhaseDuration = 45f;
    [SerializeField] private float shortStrokeDistanceThreshold = 130f;
    [SerializeField] private float livelySpeedThreshold = 280f;

    private bool contourPhaseActive;
    private float contourTimeLeft;

    private int strokeCount;
    private int shortStrokeCount;
    private int livelyStrokeCount;
    private float totalStrokeDistance;

    private int contoursScore;
    private int lightScore;
    private int strokesScore;

    private void Awake()
    {
        if (painter == null)
            painter = FindObjectOfType<Painter>();

        ShowIntro();
    }

    private void OnEnable()
    {
        if (painter != null)
            painter.OnStrokeFinished += OnStrokeFinished;
    }

    private void OnDisable()
    {
        if (painter != null)
            painter.OnStrokeFinished -= OnStrokeFinished;
    }

    private void Update()
    {
        if (!contourPhaseActive)
            return;

        contourTimeLeft -= Time.deltaTime;

        if (timerText != null)
            timerText.text = $"Время: {Mathf.CeilToInt(Mathf.Max(0f, contourTimeLeft))}";

        if (contourTimeLeft <= 0f)
            CompleteContourPhase();
    }

    public void ShowIntro()
    {
        SetScreen(parisIntroPanel, true);
        SetScreen(gameplayPanel, false);
        SetScreen(resultPanel, false);

        if (painter != null)
            painter.DisableDrawing();

        contourPhaseActive = false;
    }

    public void StartFromIntro()
    {
        SetScreen(parisIntroPanel, false);
        SetScreen(gameplayPanel, true);
        SetScreen(resultPanel, false);

        StartContourPhase();
    }

    private void StartContourPhase()
    {
        ResetContourStats();

        if (phaseText != null)
            phaseText.text = "Фаза 1/3: Контуры";

        if (instructionText != null)
        {
            instructionText.text =
                "Делай короткие и быстрые штрихи по контурам. " +
                "Длинные ровные линии дают меньше очков.";
        }

        if (painter != null)
        {
            painter.ClearCanvas();
            painter.SetBrushType(Painter.BrushType.Regular);
            painter.EnableDrawing();
        }

        contourTimeLeft = contourPhaseDuration;
        contourPhaseActive = true;
    }

    private void CompleteContourPhase()
    {
        contourPhaseActive = false;

        if (painter != null)
            painter.DisableDrawing();

        contoursScore = CalculateContourScore();

        lightScore = 0;
        strokesScore = 0;

        int totalScore = contoursScore + lightScore + strokesScore;
        string badge = ResolveBadge();

        if (contoursScoreText != null)
            contoursScoreText.text = $"Контуры: {contoursScore}/100";

        if (totalScoreText != null)
            totalScoreText.text = $"Итог: {totalScore}/300";

        if (badgeText != null)
            badgeText.text = $"Награда: {badge}";

        if (resultCommentText != null)
            resultCommentText.text = BuildResultComment();

        SetScreen(gameplayPanel, false);
        SetScreen(resultPanel, true);
    }

    public void RestartContourPhase()
    {
        SetScreen(resultPanel, false);
        SetScreen(gameplayPanel, true);
        StartContourPhase();
    }

    private void OnStrokeFinished(Painter.StrokeSummary summary)
    {
        if (!contourPhaseActive)
            return;

        if (summary.sampledPoints <= 1)
            return;

        strokeCount++;
        totalStrokeDistance += summary.distancePixels;

        if (summary.distancePixels <= shortStrokeDistanceThreshold)
            shortStrokeCount++;

        if (summary.averageSpeed >= livelySpeedThreshold)
            livelyStrokeCount++;
    }

    private int CalculateContourScore()
    {
        if (strokeCount == 0)
            return 0;

        float shortRatio = (float)shortStrokeCount / strokeCount;
        float livelyRatio = (float)livelyStrokeCount / strokeCount;
        float averageDistance = totalStrokeDistance / strokeCount;

        float shortScore = Mathf.Clamp01((shortRatio - 0.35f) / 0.65f) * 45f;
        float livelyScore = Mathf.Clamp01((livelyRatio - 0.25f) / 0.75f) * 35f;
        float densityScore = Mathf.Clamp01(strokeCount / 22f) * 20f;

        float longStrokePenalty =
            Mathf.Clamp01((averageDistance - shortStrokeDistanceThreshold) / shortStrokeDistanceThreshold) * 18f;

        return Mathf.Clamp(
            Mathf.RoundToInt(shortScore + livelyScore + densityScore - longStrokePenalty),
            0,
            100
        );
    }

    private string ResolveBadge()
    {
        if (contoursScore >= lightScore && contoursScore >= strokesScore)
            return "Наблюдатель формы";

        if (lightScore >= contoursScore && lightScore >= strokesScore)
            return "Наблюдатель света";

        return "Наблюдатель ритма";
    }

    private string BuildResultComment()
    {
        if (strokeCount == 0)
            return "Ты почти не работал с контурами. Попробуй короткие энергичные штрихи.";

        if (contoursScore >= 80)
            return "Сильный импрессионистский контур: живо, фрагментарно и с ритмом.";

        if (contoursScore >= 50)
            return "Хорошее начало. Добавь больше коротких штрихов и меньше длинных линий.";

        return "Сейчас контур еще академичный. Делай мазки короче и быстрее.";
    }

    private void ResetContourStats()
    {
        strokeCount = 0;
        shortStrokeCount = 0;
        livelyStrokeCount = 0;
        totalStrokeDistance = 0f;
    }

    private static void SetScreen(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
