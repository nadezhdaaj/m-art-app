using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TreasureChestReveal : MonoBehaviour
{
    [System.Serializable]
    public class RewardTier
    {
        public int minCorrectAnswers;
        public Sprite icon;
        public string title;
        [TextArea(2, 3)]
        public string description;
        [TextArea(2, 3)]
        public string chestSpeech;
    }

    [Header("UI")]
    public GameObject panel;
    public TMP_Text hintText;
    public Button chestButton;
    public Image chestImage;
    public RectTransform chestShakeTarget;
    public TMP_Text chestSpeechText;
    public GameObject chestSpeechBackground;
    public GameObject artifactCard;
    public GameObject artifactImage;
    public Image artifactImageGraphic;
    public TMP_Text artifactTitle;
    public TMP_Text artifactDescription;
    public TMP_Text scoreText;
    public Button againButton;

    [Header("Chest Sprites")]
    public Sprite closedSprite;
    public Sprite halfOpenSprite;
    public Sprite openSprite;

    [Header("Reward Tiers (sort by minCorrectAnswers, lowest first)")]
    public RewardTier[] rewardTiers;

    [Header("Timing")]
    public float shakeDuration = 0.8f;
    public float shakeAmplitude = 18f;
    public float halfOpenPause = 0.35f;
    public float delayBeforeReward = 1f;

    QuizManager quizManager;
    Vector2 chestRestPosition;
    int correctAnswers;
    int totalQuestions;
    float earnedPoints;
    bool isOpening;
    bool hasOpened;
    bool isInitialized;

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        if (isInitialized)
            return;

        isInitialized = true;

        if (panel == null)
            panel = gameObject;

        if (chestShakeTarget == null && chestImage != null)
            chestShakeTarget = chestImage.rectTransform;

        if (artifactImageGraphic == null && artifactImage != null)
            artifactImageGraphic = artifactImage.GetComponent<Image>();

        if (quizManager == null)
            quizManager = FindObjectOfType<QuizManager>();

        EnsureRewardTiers();
        EnsureChestSprites();

        if (chestButton != null)
        {
            chestButton.onClick.RemoveListener(OnChestClicked);
            chestButton.onClick.AddListener(OnChestClicked);
        }

        if (againButton != null)
        {
            againButton.onClick.RemoveListener(OnAgainClicked);
            againButton.onClick.AddListener(OnAgainClicked);
        }
    }

    void Start()
    {
        Initialize();

        if (chestShakeTarget != null)
            chestRestPosition = chestShakeTarget.anchoredPosition;
    }

    public void Show(int correct, int total, float points)
    {
        Initialize();

        correctAnswers = correct;
        totalQuestions = total;
        earnedPoints = points;
        isOpening = false;
        hasOpened = false;

        if (chestShakeTarget != null)
            chestRestPosition = chestShakeTarget.anchoredPosition;

        ResetChestVisuals();

        if (panel != null)
            panel.SetActive(true);
    }

    public void HidePanel()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    void ResetChestVisuals()
    {
        if (hintText != null)
            hintText.gameObject.SetActive(true);

        SetChestSpeechVisible(false);

        if (artifactCard != null)
            artifactCard.SetActive(false);

        SetArtifactContentVisible(false);

        if (againButton != null)
            againButton.gameObject.SetActive(false);

        SetChestSprite(closedSprite);

        if (chestButton != null)
            chestButton.interactable = true;

        if (chestShakeTarget != null)
            chestShakeTarget.anchoredPosition = chestRestPosition;
    }

    void OnChestClicked()
    {
        if (isOpening || hasOpened)
            return;

        StartCoroutine(OpenChestSequence());
    }

    IEnumerator OpenChestSequence()
    {
        isOpening = true;

        if (chestButton != null)
            chestButton.interactable = false;

        if (hintText != null)
            hintText.gameObject.SetActive(false);

        yield return ShakeChest();
        yield return new WaitForSeconds(halfOpenPause);

        RewardTier tier = PickRewardTier(correctAnswers, totalQuestions);

        if (chestSpeechText != null)
            chestSpeechText.text = tier.chestSpeech;

        SetChestSpeechVisible(true);
        yield return new WaitForSeconds(delayBeforeReward);

        SetChestSprite(openSprite);
        RevealReward(tier);

        hasOpened = true;
        isOpening = false;

        if (againButton != null)
            againButton.gameObject.SetActive(true);
    }

    IEnumerator ShakeChest()
    {
        if (chestShakeTarget == null)
            yield break;

        float elapsed = 0f;
        bool switchedToHalfOpen = false;
        float halfOpenAt = shakeDuration * 0.55f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            if (!switchedToHalfOpen && elapsed >= halfOpenAt)
            {
                SetChestSprite(halfOpenSprite);
                switchedToHalfOpen = true;
            }

            float dampening = 1f - (elapsed / shakeDuration);
            float offset = Mathf.Sin(elapsed * 40f) * shakeAmplitude * dampening;
            chestShakeTarget.anchoredPosition = chestRestPosition + new Vector2(offset, 0f);
            yield return null;
        }

        chestShakeTarget.anchoredPosition = chestRestPosition;

        if (!switchedToHalfOpen)
            SetChestSprite(halfOpenSprite);
    }

    void RevealReward(RewardTier tier)
    {
        if (artifactTitle != null)
            artifactTitle.text = tier.title;

        if (artifactDescription != null)
            artifactDescription.text = tier.description;

        if (artifactImageGraphic != null && tier.icon != null)
            artifactImageGraphic.sprite = tier.icon;

        if (scoreText != null)
        {
            int points = Mathf.RoundToInt(earnedPoints);
            scoreText.text = "+" + points + " очков · " + correctAnswers + "/" + totalQuestions;
        }

        if (artifactCard != null)
            artifactCard.SetActive(true);

        SetArtifactContentVisible(true);
    }

    void SetArtifactContentVisible(bool visible)
    {
        if (artifactImage != null)
            artifactImage.SetActive(visible);

        if (artifactTitle != null)
            artifactTitle.gameObject.SetActive(visible);

        if (artifactDescription != null)
            artifactDescription.gameObject.SetActive(visible);

        if (scoreText != null)
            scoreText.gameObject.SetActive(visible);
    }

    void SetChestSpeechVisible(bool visible)
    {
        if (chestSpeechText != null)
            chestSpeechText.gameObject.SetActive(visible);

        if (chestSpeechBackground != null)
            chestSpeechBackground.SetActive(visible);
    }

    RewardTier PickRewardTier(int correct, int total)
    {
        EnsureRewardTiers();

        RewardTier best = rewardTiers[0];

        for (int i = 0; i < rewardTiers.Length; i++)
        {
            if (correct >= rewardTiers[i].minCorrectAnswers
                && rewardTiers[i].minCorrectAnswers >= best.minCorrectAnswers)
            {
                best = rewardTiers[i];
            }
        }

        return best;
    }

    void SetChestSprite(Sprite sprite)
    {
        if (chestImage == null || sprite == null)
            return;

        chestImage.sprite = sprite;
    }

    void EnsureChestSprites()
    {
        if (closedSprite == null && chestImage != null)
            closedSprite = chestImage.sprite;

        if (halfOpenSprite == null)
            Debug.LogWarning("TreasureChestReveal: назначь Half Open Sprite в инспекторе.");

        if (openSprite == null)
            Debug.LogWarning("TreasureChestReveal: назначь Open Sprite в инспекторе.");
    }

    void OnAgainClicked()
    {
        HidePanel();

        if (quizManager != null)
            quizManager.Restart();
    }

    void EnsureRewardTiers()
    {
        if (rewardTiers != null && rewardTiers.Length > 0)
            return;

        rewardTiers = new RewardTier[]
        {
            new RewardTier
            {
                minCorrectAnswers = 0,
                title = "Гость с путеводителем",
                description = "Ты дошёл до конца — это уже победа.",
                chestSpeech = "Ну… держи. Главное — не сдался!"
            },
            new RewardTier
            {
                minCorrectAnswers = 3,
                title = "Зритель со стикером",
                description = "Не всё запомнил, но музей ты прошёл.",
                chestSpeech = "Неплохо! Сундук одобряет."
            },
            new RewardTier
            {
                minCorrectAnswers = 6,
                title = "Внимательный зритель",
                description = "Уже можешь рассказать друзьям пару фактов.",
                chestSpeech = "О, ты реально старался!"
            },
            new RewardTier
            {
                minCorrectAnswers = 9,
                title = "Почти куратор",
                description = "Куратор бы взял тебя на экскурсию.",
                chestSpeech = "Класс! Почти идеал!"
            },
            new RewardTier
            {
                minCorrectAnswers = 12,
                title = "Куратор М'АРТ",
                description = "12 из 12. Ты опасен для экскурсий.",
                chestSpeech = "12 из 12?! Я в шоке. Береги это."
            }
        };
    }
}
