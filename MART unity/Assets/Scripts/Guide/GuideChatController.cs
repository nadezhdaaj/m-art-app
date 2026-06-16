using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Чат Мартишки: GET /guide/suggestions, POST /guide/chat (+ offline fallback).
/// Раскладку UI настраивайте вручную в сцене — скрипт не двигает элементы.
/// </summary>
public class GuideChatController : MonoBehaviour
{
    private const string PracticalInfoButtonName = "?";
    private const string VisitorInfoTopicId = "visitor_info";

    private const string PracticalInfoUserLabel = "Практическая информация о музее";

    [Header("UI")]
    [SerializeField] private TMP_Text welcomeText;
    [SerializeField] private TMP_Text chatLogText;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button sendButton;

    [Header("Панель подсказок (Leading questions)")]
    [SerializeField] private GameObject leadingQuestionsPanel;
    [SerializeField] private Button leadingQuestionsButton;

    [Header("Панель additions")]
    [SerializeField] private GameObject additionsPanel;
    [SerializeField] private Button additionsButton;

    [Header("Область диалога (обрезка по auxiliary panel)")]
    [SerializeField] private RectTransform auxiliaryPanel;
    [SerializeField] private ScrollRect dialogueScrollRect;
    [SerializeField] private RectTransform dialogueScrollContent;
    [SerializeField] private float dialogueSidePadding = 8f;
    [SerializeField] private float dialogueEdgeGap = 12f;
    [SerializeField] private float dialogueBottomFallback = 300f;
    [SerializeField] private float dialogueTopFallback = 100f;
    [Tooltip("Ваш scrollbar из сцены (объект scrollbar на панели чата)")]
    [SerializeField] private GameObject dialogueScrollbarRoot;
    [SerializeField] private DialogueScrollThumb dialogueScrollThumb;
    [SerializeField] private float dialogueScrollbarWidth = 10f;
    [SerializeField] private float dialogueScrollbarHandleHeight = 72f;
    [SerializeField] private float dialogueScrollbarEdgePadding = 10f;

    [Tooltip("Новые сообщения прокручивают вниз только если пользователь уже внизу диалога")]
    [SerializeField] private bool autoScrollWhenAtBottom = true;
    [SerializeField] private float scrollAtBottomThreshold = 0.08f;

    [Header("Чипы-подсказки")]
    [SerializeField] private Transform chipsContainer;
    [SerializeField] private Button chipButtonPrefab;
    [SerializeField] private int chipColumns = 3;
    [SerializeField] private Vector2 chipCellSize = new Vector2(350f, 72f);
    [SerializeField] private Vector2 chipSpacing = new Vector2(14f, 12f);
    [Tooltip("Кнопки лежат в Chips в сцене — видны в Hierarchy всегда, скрипт только обновляет текст")]
    [SerializeField] private bool useSceneChips = true;
    [Tooltip("Подставлять подписи с сервера/json. Выключите, если текст на кнопках правите только в сцене.")]
    [SerializeField] private bool syncChipLabelsFromData = true;

    [Header("Настройки")]
    [Tooltip("Если сервер недоступен — ответы из Resources/Guide/guide.json")]
    [SerializeField] private bool useOfflineFallback = true;

    private GuideDataBundle offlineData;
    private bool loadedOnce;
    private bool serverOnline;
    private bool welcomeDismissed;
    private bool followLatestMessages = true;
    private bool practicalInfoVisible;
    private string practicalInfoUserLine;
    private string practicalInfoGuideLine;
    private Coroutine practicalInfoRoutine;
    private Coroutine scrollBottomRoutine;
    private GameObject chipPrefabAsset;

    private static string SuggestionsUrl => AppConfig.BaseUrl + "/guide/suggestions";
    private static string ChatUrl => AppConfig.BaseUrl + "/guide/chat";

    private void Awake()
    {
        ResolveReferences();

        if (useOfflineFallback)
            LoadOfflineBundle();

        chipPrefabAsset = Resources.Load<GameObject>("Guide/GuideChip");
        HideChipTemplate();
        ConfigureAuxiliaryPanelRaycasts();
        ConfigureChatPanelRaycasts();

        if (welcomeText is Graphic welcomeGraphic)
            welcomeGraphic.raycastTarget = false;
    }

    private void Start()
    {
        ResolveReferences();
        WireLeadingQuestionsButton();
        StartCoroutine(SetupDialogueScrollWhenReady());
    }

    private void ResolveReferences()
    {
        Transform leadingQuestionsRoot = FindDeepChild(transform, "Leading questions");

        if (leadingQuestionsPanel == null && leadingQuestionsRoot != null)
            leadingQuestionsPanel = leadingQuestionsRoot.gameObject;

        if (leadingQuestionsButton == null)
        {
            Transform button = FindDeepChild(transform, "Leading questions button");
            if (button != null)
                leadingQuestionsButton = button.GetComponent<Button>();
        }

        if (additionsPanel == null)
        {
            Transform additions = FindDeepChild(transform, "additions");
            if (additions != null)
                additionsPanel = additions.gameObject;
        }

        if (additionsButton == null)
        {
            Transform button = FindDeepChild(transform, "additions button");
            if (button != null)
                additionsButton = button.GetComponent<Button>();
        }

        if (auxiliaryPanel == null)
        {
            Transform aux = FindDeepChild(transform, "auxiliary panel");
            if (aux != null)
                auxiliaryPanel = aux as RectTransform;
        }

        if (chipsContainer == null)
        {
            if (leadingQuestionsRoot != null)
            {
                Transform chips = leadingQuestionsRoot.Find("Chips");
                if (chips != null)
                    chipsContainer = chips;
            }

            if (chipsContainer == null)
            {
                Transform chips = transform.Find("Chips");
                if (chips != null)
                    chipsContainer = chips;
            }
        }

        if (chipButtonPrefab == null)
        {
            Transform template = null;
            if (leadingQuestionsRoot != null)
                template = leadingQuestionsRoot.Find("ChipTemplate button");

            if (template == null)
                template = FindDeepChild(transform, "ChipTemplate button");

            if (template != null)
                chipButtonPrefab = template.GetComponent<Button>();
        }

        if (dialogueScrollRect == null)
        {
            Transform existing = FindDeepChild(transform, "DialogueScrollView");
            if (existing != null)
                dialogueScrollRect = existing.GetComponent<ScrollRect>();
        }

        if (dialogueScrollContent == null && dialogueScrollRect != null)
            dialogueScrollContent = dialogueScrollRect.content;

        if (dialogueScrollbarRoot == null)
        {
            Transform scrollbar = FindDeepChild(transform, "scrollbar");
            if (scrollbar != null)
                dialogueScrollbarRoot = scrollbar.gameObject;
        }

        if (dialogueScrollThumb == null && dialogueScrollbarRoot != null)
            dialogueScrollThumb = dialogueScrollbarRoot.GetComponentInChildren<DialogueScrollThumb>(true);
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private void WireLeadingQuestionsButton()
    {
        if (leadingQuestionsButton == null)
        {
            Transform button = FindDeepChild(transform, "Leading questions button");
            if (button != null)
                leadingQuestionsButton = button.GetComponent<Button>();
        }

        if (leadingQuestionsButton == null)
        {
            Debug.LogWarning("Guide: кнопка Leading questions button не найдена.");
            return;
        }

        leadingQuestionsButton.onClick.RemoveListener(ToggleLeadingQuestionsPanel);
        leadingQuestionsButton.onClick.AddListener(ToggleLeadingQuestionsPanel);
    }

    private void WireAdditionsButton()
    {
        if (additionsButton == null)
        {
            Transform button = FindDeepChild(transform, "additions button");
            if (button != null)
                additionsButton = button.GetComponent<Button>();
        }

        if (additionsButton == null)
        {
            Debug.LogWarning("Guide: кнопка additions button не найдена.");
            return;
        }

        additionsButton.onClick.RemoveListener(ToggleAdditionsPanel);
        additionsButton.onClick.AddListener(ToggleAdditionsPanel);
    }

    private void WireAdditionsPanelButtons()
    {
        if (additionsPanel == null)
            return;

        foreach (Button button in additionsPanel.GetComponentsInChildren<Button>(true))
        {
            button.onClick.RemoveListener(CloseAdditionsPanel);
            button.onClick.RemoveListener(OnPracticalInfoClicked);

            if (button.gameObject.name == PracticalInfoButtonName)
            {
                button.onClick.AddListener(OnPracticalInfoClicked);
                continue;
            }

            button.onClick.AddListener(CloseAdditionsPanel);
        }
    }

    private void UnwireAdditionsPanelButtons()
    {
        if (additionsPanel == null)
            return;

        foreach (Button button in additionsPanel.GetComponentsInChildren<Button>(true))
        {
            button.onClick.RemoveListener(CloseAdditionsPanel);
            button.onClick.RemoveListener(OnPracticalInfoClicked);
        }
    }

    private void OnPracticalInfoClicked()
    {
        CloseAdditionsPanel();

        if (practicalInfoVisible)
        {
            HidePracticalInfo();
            return;
        }

        if (practicalInfoRoutine != null)
            StopCoroutine(practicalInfoRoutine);

        practicalInfoRoutine = StartCoroutine(ShowPracticalInfoCoroutine());
    }

    private IEnumerator ShowPracticalInfoCoroutine()
    {
        HideWelcomeText();
        followLatestMessages = true;

        string reply = null;
        yield return FetchTopicReplyCoroutine(VisitorInfoTopicId, value => reply = value);

        practicalInfoRoutine = null;

        if (string.IsNullOrEmpty(reply))
            yield break;

        practicalInfoUserLine = FormatChatLine(UserAuthor, PracticalInfoUserLabel);
        practicalInfoGuideLine = FormatChatLine(GuideAuthor, reply);
        practicalInfoVisible = true;

        AppendLine(UserAuthor, PracticalInfoUserLabel);
        AppendLine(GuideAuthor, reply);
    }

    private IEnumerator FetchTopicReplyCoroutine(string topicId, Action<string> assignReply)
    {
        if (!serverOnline && TryOfflineTopic(topicId, out string offlineReply))
        {
            assignReply?.Invoke(offlineReply);
            yield break;
        }

        GuideChatTopicRequest body = new GuideChatTopicRequest { topicId = topicId };
        string jsonBody = JsonUtility.ToJson(body);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using UnityWebRequest request = new UnityWebRequest(ChatUrl, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            if (useOfflineFallback && TryOfflineMessageFromJsonBody(jsonBody, out string fallback))
                assignReply?.Invoke(fallback);
            else
                assignReply?.Invoke(null);

            yield break;
        }

        GuideChatResponseDto response =
            JsonUtility.FromJson<GuideChatResponseDto>(request.downloadHandler.text);

        assignReply?.Invoke(response != null ? response.reply : null);
    }

    private void HidePracticalInfo()
    {
        if (chatLogText == null || !practicalInfoVisible)
            return;

        string text = chatLogText.text;
        string pair = practicalInfoUserLine + "\n\n" + practicalInfoGuideLine;
        string updated = RemoveChatBlock(text, pair);

        if (updated == text)
        {
            updated = RemoveChatBlock(text, practicalInfoGuideLine);
            updated = RemoveChatBlock(updated, practicalInfoUserLine);
        }

        chatLogText.text = updated;
        practicalInfoVisible = false;
        practicalInfoUserLine = null;
        practicalInfoGuideLine = null;

        chatLogText.ForceMeshUpdate(true);
        SyncDialogueScrollContentSize();

        if (string.IsNullOrEmpty(chatLogText.text))
        {
            welcomeDismissed = false;
            ShowWelcomeText();
            HideDialogueScrollbar();
            return;
        }

        RequestScrollToBottom();
    }

    private static string RemoveChatBlock(string text, string block)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(block))
            return text ?? "";

        if (text == block)
            return "";

        const string gap = "\n\n";
        string withGapBefore = gap + block;
        string withGapAfter = block + gap;

        if (text.Contains(withGapBefore))
            return text.Replace(withGapBefore, "");

        if (text.Contains(withGapAfter))
            return text.Replace(withGapAfter, "");

        return text.Replace(block, "");
    }

    private void ConfigureAuxiliaryPanelRaycasts()
    {
        if (auxiliaryPanel == null)
            return;

        Image panelImage = auxiliaryPanel.GetComponent<Image>();
        if (panelImage != null)
            panelImage.raycastTarget = false;
    }

    private void ConfigureChatPanelRaycasts()
    {
        Image panelImage = GetComponent<Image>();
        if (panelImage != null)
            panelImage.raycastTarget = false;
    }

    private void HideChipTemplate()
    {
        if (chipButtonPrefab != null)
            chipButtonPrefab.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        HideChipTemplate();
        ConfigureAuxiliaryPanelRaycasts();
        ConfigureChatPanelRaycasts();
        WireLeadingQuestionsButton();
        WireAdditionsButton();
        WireAdditionsPanelButtons();

        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendClicked);
            sendButton.onClick.AddListener(OnSendClicked);
        }

        ResetChatSession();

        // Сразу показываем чипы (не ждём сеть)
        ApplySuggestions(BuildSuggestionsFromOffline());
        StartCoroutine(LoadSuggestions());
        StartCoroutine(SetupDialogueScrollWhenReady());
    }

    private void OnDisable()
    {
        if (sendButton != null)
            sendButton.onClick.RemoveListener(OnSendClicked);

        if (leadingQuestionsButton != null)
            leadingQuestionsButton.onClick.RemoveListener(ToggleLeadingQuestionsPanel);

        if (additionsButton != null)
            additionsButton.onClick.RemoveListener(ToggleAdditionsPanel);

        UnwireAdditionsPanelButtons();
        UnwireDialogueScrollFollowLatest();
    }

    private void LoadOfflineBundle()
    {
        TextAsset asset = Resources.Load<TextAsset>("Guide/guide");
        if (asset == null)
        {
            Debug.LogWarning("Guide: Resources/Guide/guide.json не найден.");
            return;
        }

        offlineData = JsonUtility.FromJson<GuideDataBundle>(asset.text);
    }

    private void ResetChatSession()
    {
        welcomeDismissed = false;
        followLatestMessages = true;
        ClearChat();
        ShowWelcomeText();
        CloseLeadingQuestionsPanel();
        CloseAdditionsPanel();
        HideDialogueScrollbar();
    }

    private void ToggleLeadingQuestionsPanel()
    {
        if (leadingQuestionsPanel == null)
        {
            ResolveReferences();
            if (leadingQuestionsPanel == null)
            {
                Debug.LogWarning("Guide: панель Leading questions не найдена.");
                return;
            }
        }

        bool willOpen = !leadingQuestionsPanel.activeSelf;
        leadingQuestionsPanel.SetActive(willOpen);

        if (willOpen)
            BringLeadingQuestionsToFront();
    }

    private void ToggleAdditionsPanel()
    {
        if (additionsPanel == null)
        {
            ResolveReferences();
            if (additionsPanel == null)
            {
                Debug.LogWarning("Guide: панель additions не найдена.");
                return;
            }
        }

        bool willOpen = !additionsPanel.activeSelf;
        additionsPanel.SetActive(willOpen);

        if (willOpen)
        {
            WireAdditionsPanelButtons();
            BringAdditionsToFront();
        }
    }

    private void BringLeadingQuestionsToFront()
    {
        if (leadingQuestionsPanel == null)
            return;

        leadingQuestionsPanel.transform.SetAsLastSibling();

        Image panelImage = leadingQuestionsPanel.GetComponent<Image>();
        if (panelImage != null)
            panelImage.raycastTarget = false;
    }

    private void BringAdditionsToFront()
    {
        if (additionsPanel == null)
            return;

        additionsPanel.transform.SetAsLastSibling();

        Image panelImage = additionsPanel.GetComponent<Image>();
        if (panelImage != null)
            panelImage.raycastTarget = false;
    }

    private void CloseAdditionsPanel()
    {
        if (additionsPanel != null)
            additionsPanel.SetActive(false);
    }

    private IEnumerator SetupDialogueScrollWhenReady()
    {
        for (int i = 0; i < 3; i++)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        EnsureDialogueScrollClip();
        RefreshDialogueScrollBounds();
        SyncDialogueScrollContentSize();

        yield return null;
        Canvas.ForceUpdateCanvases();
        RefreshDialogueScrollBounds();
        SyncDialogueScrollContentSize();
    }

    private void EnsureDialogueScrollClip()
    {
        if (chatLogText == null)
            return;

        RectTransform chatRoot = transform as RectTransform;
        if (chatRoot == null)
            return;

        ResetBrokenDialogueScrollView(chatRoot);

        if (dialogueScrollRect == null)
        {
            Transform existing = FindDeepChild(chatRoot, "DialogueScrollView");
            if (existing != null)
                dialogueScrollRect = existing.GetComponent<ScrollRect>();
        }

        if (dialogueScrollRect != null && dialogueScrollRect.content != null)
        {
            dialogueScrollContent = dialogueScrollRect.content;
            EnsureTextInsideScrollContent();
            ConfigureDialogueTextForScroll(chatLogText.rectTransform);
            ConfigureScrollInteraction(dialogueScrollRect);
            WireDialogueScrollFollowLatest(dialogueScrollRect);
            ConfigureDialogueViewportForScrollbar(dialogueScrollRect);
            BindCustomDialogueScrollbar(dialogueScrollRect);
            SyncDialogueScrollContentSize();
            return;
        }

        GameObject scrollGo = new GameObject(
            "DialogueScrollView",
            typeof(RectTransform),
            typeof(ScrollRect)
        );
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.SetParent(chatRoot, false);

        Transform aux = auxiliaryPanel != null ? auxiliaryPanel : FindDeepChild(chatRoot, "auxiliary panel");
        if (aux != null)
            scrollRt.SetSiblingIndex(aux.GetSiblingIndex());

        GameObject viewportGo = new GameObject(
            "Viewport",
            typeof(RectTransform),
            typeof(RectMask2D),
            typeof(Image)
        );
        RectTransform viewportRt = viewportGo.GetComponent<RectTransform>();
        viewportRt.SetParent(scrollRt, false);
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;

        Image viewportImage = viewportGo.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.08f);
        viewportImage.raycastTarget = true;

        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        RectTransform contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.SetParent(viewportRt, false);
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 100f);

        RectTransform textRt = chatLogText.rectTransform;
        textRt.SetParent(contentRt, false);
        ConfigureDialogueTextForScroll(textRt);

        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.viewport = viewportRt;
        scroll.content = contentRt;
        ConfigureScrollInteraction(scroll);
        WireDialogueScrollFollowLatest(scroll);
        ApplyDialogueScrollAreaInsets(scrollRt, chatRoot);
        ConfigureDialogueViewportForScrollbar(scroll);
        BindCustomDialogueScrollbar(scroll);

        dialogueScrollRect = scroll;
        dialogueScrollContent = contentRt;
    }

    private void ResetBrokenDialogueScrollView(RectTransform chatRoot)
    {
        if (dialogueScrollRect == null)
            return;

        if (GetDialogueViewportHeight() >= 80f)
            return;

        if (dialogueScrollbarRoot != null)
            dialogueScrollbarRoot.transform.SetParent(chatRoot, false);

        Destroy(dialogueScrollRect.gameObject);
        dialogueScrollRect = null;
        dialogueScrollContent = null;
    }

    private void ConfigureDialogueViewportForScrollbar(ScrollRect scroll)
    {
        if (scroll?.viewport == null)
            return;

        RectTransform viewport = scroll.viewport;
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;
    }

    private static void ConfigureScrollInteraction(ScrollRect scroll)
    {
        if (scroll == null)
            return;

        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        scroll.elasticity = 0.08f;

        if (scroll.viewport != null)
        {
            Image viewportImage = scroll.viewport.GetComponent<Image>();
            if (viewportImage != null)
            {
                viewportImage.raycastTarget = true;
                if (viewportImage.color.a < 0.05f)
                    viewportImage.color = new Color(1f, 1f, 1f, 0.08f);
            }
        }
    }

    private void BindCustomDialogueScrollbar(ScrollRect scroll)
    {
        if (scroll == null)
            return;

        EnsureFloatingScrollThumb();

        if (dialogueScrollbarRoot == null)
            return;

        SetupFloatingScrollThumb(scroll);
        UpdateDialogueScrollbarVisibility();
    }

    private void SetupFloatingScrollThumb(ScrollRect scroll)
    {
        if (scroll == null || dialogueScrollbarRoot == null)
            return;

        RectTransform scrollRt = scroll.transform as RectTransform;
        RectTransform thumbRt = dialogueScrollbarRoot.transform as RectTransform;
        RectTransform thumbParent = scroll.viewport != null ? scroll.viewport : scrollRt;
        if (scrollRt == null || thumbRt == null || thumbParent == null)
            return;

        thumbRt.SetParent(thumbParent, false);
        thumbRt.anchorMin = new Vector2(1f, 1f);
        thumbRt.anchorMax = new Vector2(1f, 1f);
        thumbRt.pivot = new Vector2(1f, 1f);
        thumbRt.sizeDelta = new Vector2(dialogueScrollbarWidth, dialogueScrollbarHandleHeight);
        thumbRt.anchoredPosition = new Vector2(-dialogueScrollbarEdgePadding, -dialogueScrollbarEdgePadding);
        thumbRt.SetAsLastSibling();

        Image trackImage = dialogueScrollbarRoot.GetComponent<Image>();
        if (trackImage != null)
        {
            trackImage.enabled = true;
            trackImage.color = new Color(0f, 0f, 0f, 0.001f);
            trackImage.raycastTarget = true;
        }

        Scrollbar legacyScrollbar = dialogueScrollbarRoot.GetComponent<Scrollbar>();
        if (legacyScrollbar != null)
            legacyScrollbar.enabled = false;

        Transform handleTransform = thumbRt.Find("Handle")
            ?? thumbRt.Find("ScrollHandle")
            ?? thumbRt.Find("handle");

        if (handleTransform is RectTransform configuredHandle)
        {
            configuredHandle.anchorMin = Vector2.zero;
            configuredHandle.anchorMax = Vector2.one;
            configuredHandle.offsetMin = Vector2.zero;
            configuredHandle.offsetMax = Vector2.zero;

            Image handleImage = configuredHandle.GetComponent<Image>();
            if (handleImage != null)
                handleImage.raycastTarget = false;

            DialogueScrollThumb legacyThumb = configuredHandle.GetComponent<DialogueScrollThumb>();
            if (legacyThumb != null)
                Destroy(legacyThumb);
        }

        dialogueScrollThumb = thumbRt.GetComponent<DialogueScrollThumb>();
        if (dialogueScrollThumb == null)
            dialogueScrollThumb = thumbRt.gameObject.AddComponent<DialogueScrollThumb>();

        dialogueScrollThumb.Configure(scroll, thumbRt, dialogueScrollbarEdgePadding);
    }

    private void EnsureFloatingScrollThumb()
    {
        if (dialogueScrollbarRoot == null)
        {
            Transform scrollbar = FindDeepChild(transform, "scrollbar");
            if (scrollbar != null)
                dialogueScrollbarRoot = scrollbar.gameObject;
        }

        if (dialogueScrollbarRoot == null)
            return;

        Button strayButton = dialogueScrollbarRoot.GetComponent<Button>();
        if (strayButton != null)
            strayButton.enabled = false;
    }

    private void SyncFloatingScrollThumb()
    {
        if (dialogueScrollThumb != null)
            dialogueScrollThumb.SyncFromScroll();
    }

    private bool IsDialogueOverflowing()
    {
        float viewportHeight = GetDialogueViewportHeight();
        float contentHeight = GetDialogueContentHeight();

        if (viewportHeight <= 1f)
            return contentHeight > 120f;

        return contentHeight > viewportHeight + 4f;
    }

    private float GetDialogueContentHeight()
    {
        if (chatLogText != null)
        {
            chatLogText.ForceMeshUpdate(true);
            return chatLogText.preferredHeight + 24f;
        }

        if (dialogueScrollContent != null)
            return dialogueScrollContent.rect.height;

        return 0f;
    }

    private void UpdateDialogueScrollbarVisibility()
    {
        if (dialogueScrollRect == null || dialogueScrollbarRoot == null)
            return;

        bool show = IsDialogueOverflowing();
        dialogueScrollbarRoot.SetActive(show);

        if (!show)
            return;

        SetupFloatingScrollThumb(dialogueScrollRect);
        SyncFloatingScrollThumb();
    }

    private void HideDialogueScrollbar()
    {
        if (dialogueScrollbarRoot != null)
            dialogueScrollbarRoot.SetActive(false);
    }

    private void EnsureTextInsideScrollContent()
    {
        if (dialogueScrollContent == null || chatLogText == null)
            return;

        if (chatLogText.transform.parent != dialogueScrollContent)
            chatLogText.rectTransform.SetParent(dialogueScrollContent, false);
    }

    private void RefreshDialogueScrollBounds()
    {
        if (dialogueScrollRect == null)
            return;

        RectTransform scrollRt = dialogueScrollRect.transform as RectTransform;
        RectTransform chatRoot = transform as RectTransform;
        if (scrollRt == null || chatRoot == null)
            return;

        Canvas.ForceUpdateCanvases();
        ApplyDialogueScrollAreaInsets(scrollRt, chatRoot);
        SyncFloatingScrollThumb();
    }

    private void ApplyDialogueScrollAreaInsets(RectTransform scrollRt, RectTransform chatRoot)
    {
        float bottomInset = dialogueBottomFallback;
        float topInset = dialogueTopFallback;

        if (auxiliaryPanel != null)
        {
            Bounds auxBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(chatRoot, auxiliaryPanel);
            bottomInset = Mathf.Max(bottomInset, auxBounds.max.y - chatRoot.rect.yMin + dialogueEdgeGap);
        }

        if (welcomeText != null && welcomeText.gameObject.activeInHierarchy)
        {
            Bounds welcomeBounds =
                RectTransformUtility.CalculateRelativeRectTransformBounds(chatRoot, welcomeText.rectTransform);
            topInset = Mathf.Max(topInset, chatRoot.rect.yMax - welcomeBounds.min.y + dialogueEdgeGap);
        }
        else
        {
            Transform back = FindDeepChild(chatRoot, "Back");
            if (back is RectTransform backRt)
            {
                Bounds backBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(chatRoot, backRt);
                topInset = Mathf.Max(topInset, chatRoot.rect.yMax - backBounds.min.y + dialogueEdgeGap);
            }
        }

        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(dialogueSidePadding, bottomInset);
        scrollRt.offsetMax = new Vector2(-dialogueSidePadding, -topInset);
        scrollRt.SetSiblingIndex(auxiliaryPanel != null
            ? auxiliaryPanel.GetSiblingIndex()
            : scrollRt.GetSiblingIndex());
    }

    private static void ConfigureDialogueTextForScroll(RectTransform textRt)
    {
        textRt.anchorMin = new Vector2(0f, 1f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.pivot = new Vector2(0.5f, 1f);
        textRt.anchoredPosition = Vector2.zero;
        textRt.sizeDelta = new Vector2(0f, textRt.sizeDelta.y);

        TextMeshProUGUI tmp = textRt.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
            return;

        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        tmp.margin = new Vector4(12f, 8f, 12f, 8f);
    }

    private void SyncDialogueScrollContentSize()
    {
        if (chatLogText == null)
            return;

        chatLogText.ForceMeshUpdate(true);

        RectTransform textRt = chatLogText.rectTransform;
        float width = GetDialogueTextWidth();
        Vector2 preferred = chatLogText.GetPreferredValues(width, 0f);
        float height = Mathf.Max(preferred.y, chatLogText.textBounds.size.y) + 32f;
        height = Mathf.Max(height, 80f);

        textRt.sizeDelta = new Vector2(0f, height);

        if (dialogueScrollContent != null)
        {
            dialogueScrollContent.sizeDelta = new Vector2(0f, height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(dialogueScrollContent);
        }

        if (dialogueScrollRect != null)
        {
            dialogueScrollRect.enabled = true;
            dialogueScrollRect.vertical = true;
            dialogueScrollRect.horizontal = false;
        }

        Canvas.ForceUpdateCanvases();
        SyncFloatingScrollThumb();
        UpdateDialogueScrollbarVisibility();
    }

    private float GetDialogueTextWidth()
    {
        if (dialogueScrollRect != null && dialogueScrollRect.viewport != null)
        {
            float viewportWidth = dialogueScrollRect.viewport.rect.width;
            if (viewportWidth > 1f)
                return viewportWidth - 24f;
        }

        if (dialogueScrollContent != null)
        {
            float contentWidth = dialogueScrollContent.rect.width;
            if (contentWidth > 1f)
                return contentWidth;
        }

        return chatLogText.rectTransform.rect.width > 1f
            ? chatLogText.rectTransform.rect.width
            : 800f;
    }

    private float GetDialogueViewportHeight()
    {
        if (dialogueScrollRect != null && dialogueScrollRect.viewport != null)
            return dialogueScrollRect.viewport.rect.height;

        return 0f;
    }

    private void WireDialogueScrollFollowLatest(ScrollRect scroll)
    {
        if (scroll == null)
            return;

        scroll.onValueChanged.RemoveListener(OnDialogueScrollValueChanged);
        scroll.onValueChanged.AddListener(OnDialogueScrollValueChanged);
    }

    private void UnwireDialogueScrollFollowLatest()
    {
        if (dialogueScrollRect == null)
            return;

        dialogueScrollRect.onValueChanged.RemoveListener(OnDialogueScrollValueChanged);
    }

    private void OnDialogueScrollValueChanged(Vector2 _)
    {
        if (!autoScrollWhenAtBottom)
            return;

        followLatestMessages = IsDialogueNearBottom();
    }

    private bool ShouldAutoScrollAfterMessage(string author)
    {
        if (!autoScrollWhenAtBottom)
            return true;

        if (author == UserAuthor)
            return true;

        return followLatestMessages || IsDialogueNearBottom();
    }

    private void RequestScrollToBottom()
    {
        if (dialogueScrollRect == null)
            return;

        if (scrollBottomRoutine != null)
            StopCoroutine(scrollBottomRoutine);

        scrollBottomRoutine = StartCoroutine(ScrollDialogueToBottomWhenReady());
    }

    private IEnumerator ScrollDialogueToBottomWhenReady()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        SyncDialogueScrollContentSize();

        if (dialogueScrollRect != null)
        {
            dialogueScrollRect.StopMovement();
            dialogueScrollRect.verticalNormalizedPosition = 0f;
        }

        yield return null;
        Canvas.ForceUpdateCanvases();

        if (dialogueScrollRect != null)
            dialogueScrollRect.verticalNormalizedPosition = 0f;

        SyncFloatingScrollThumb();
        scrollBottomRoutine = null;
    }

    private bool IsDialogueNearBottom()
    {
        if (dialogueScrollRect == null)
            return true;

        return dialogueScrollRect.verticalNormalizedPosition <= scrollAtBottomThreshold;
    }

    private void CloseLeadingQuestionsPanel()
    {
        if (leadingQuestionsPanel != null)
            leadingQuestionsPanel.SetActive(false);
    }

    private void ClearChat()
    {
        practicalInfoVisible = false;
        practicalInfoUserLine = null;
        practicalInfoGuideLine = null;

        if (chatLogText != null)
            chatLogText.text = "";
    }

    private void ShowWelcomeText()
    {
        if (welcomeText == null || welcomeDismissed)
            return;

        welcomeText.gameObject.SetActive(true);

        if (welcomeText is Graphic welcomeGraphic)
            welcomeGraphic.raycastTarget = false;

        RefreshDialogueScrollBounds();
    }

    private void HideWelcomeText()
    {
        if (welcomeText == null || welcomeDismissed)
            return;

        welcomeDismissed = true;
        welcomeText.gameObject.SetActive(false);
        RefreshDialogueScrollBounds();
    }

    private IEnumerator LoadSuggestions()
    {
        using UnityWebRequest request = UnityWebRequest.Get(SuggestionsUrl);
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            GuideSuggestionsDto data = JsonUtility.FromJson<GuideSuggestionsDto>(request.downloadHandler.text);
            if (data?.suggestions == null || data.suggestions.Length == 0)
                data = BuildSuggestionsFromOffline();

            ApplySuggestions(data);
            serverOnline = true;
            loadedOnce = true;
            yield break;
        }

        serverOnline = false;
        Debug.LogWarning("Guide: сервер недоступен, offline. " + request.error);

        if (useOfflineFallback && offlineData != null)
        {
            ApplySuggestions(new GuideSuggestionsDto
            {
                welcome = offlineData.welcome,
                suggestions = offlineData.suggestions
            });
            if (!loadedOnce)
                AppendLine("Мартишка", "(локальный режим — запустите node index.js для полного режима)");
            loadedOnce = true;
        }
        else
        {
            ApplySuggestions(BuildSuggestionsFromOffline());
            AppendLine("Мартишка", "Не удалось загрузить подсказки: " + request.error);
        }
    }

    private GuideSuggestionsDto BuildSuggestionsFromOffline()
    {
        if (offlineData?.suggestions != null && offlineData.suggestions.Length > 0)
        {
            return new GuideSuggestionsDto
            {
                welcome = offlineData.welcome,
                suggestions = offlineData.suggestions
            };
        }

        return new GuideSuggestionsDto
        {
            welcome = "Привет! Я Мартишка. Спроси про музей или выбери подсказку.",
            suggestions = new[]
            {
                new GuideSuggestionItem { topicId = "museum", label = "Что особенного в музее?" },
                new GuideSuggestionItem { topicId = "artists", label = "Кто художники в экспозиции?" },
                new GuideSuggestionItem { topicId = "residency", label = "Что такое арт-резиденция?" },
                new GuideSuggestionItem { topicId = "events", label = "Какие мероприятия бывают?" },
                new GuideSuggestionItem { topicId = "building", label = "Расскажи про здание музея" },
                new GuideSuggestionItem { topicId = "photo", label = "Можно ли фотографировать?" }
            }
        };
    }

    private void ApplySuggestions(GuideSuggestionsDto data)
    {
        if (data == null)
            data = BuildSuggestionsFromOffline();

        if (welcomeText != null)
        {
            welcomeText.text = data.welcome ?? "";
            ShowWelcomeText();
        }

        if (chipsContainer == null)
        {
            Debug.LogError("Guide: укажите Chips Container в Inspector.");
            return;
        }

        if (data.suggestions == null || data.suggestions.Length == 0)
        {
            Debug.LogError("Guide: список подсказок пуст.");
            return;
        }

        SetupChipsGrid(data.suggestions.Length);

        if (useSceneChips && TryBindSceneChips(data))
        {
            StartCoroutine(RefreshChipLabelsAfterLayout());
            return;
        }

        ClearRuntimeChips();

        int created = 0;
        foreach (GuideSuggestionItem item in data.suggestions)
        {
            if (item == null || string.IsNullOrEmpty(item.topicId))
                continue;

            Button chip = SpawnChipButton(item);
            if (chip == null)
                continue;

            string topicId = item.topicId;
            string labelText = item.label;
            chip.onClick.AddListener(() => StartCoroutine(SendTopic(topicId, labelText)));
            created++;
        }

        Debug.Log("Guide: создано чипов (runtime): " + created);

        if (chipsContainer is RectTransform chipsRectEnd)
            LayoutRebuilder.ForceRebuildLayoutImmediate(chipsRectEnd);
    }

    private void ClearRuntimeChips()
    {
        for (int i = chipsContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = chipsContainer.GetChild(i);
            if (child.name.Contains("ChipTemplate"))
                continue;

            Destroy(child.gameObject);
        }
    }

    private bool TryBindSceneChips(GuideSuggestionsDto data)
    {
        Dictionary<string, Button> byTopicId = CollectSceneChipsByTopicId();
        if (byTopicId.Count == 0)
            return false;

        var usedTopicIds = new HashSet<string>();
        int bound = 0;

        foreach (GuideSuggestionItem item in data.suggestions)
        {
            if (item == null || string.IsNullOrEmpty(item.topicId))
                continue;

            if (!byTopicId.TryGetValue(item.topicId, out Button chip))
                continue;

            chip.gameObject.SetActive(true);
            usedTopicIds.Add(item.topicId);

            if (syncChipLabelsFromData)
                ApplyChipLabel(chip.gameObject, item.label);

            chip.onClick.RemoveAllListeners();
            string topicId = item.topicId;
            string labelText = item.label;
            chip.onClick.AddListener(() => StartCoroutine(SendTopic(topicId, labelText)));
            bound++;
        }

        foreach (KeyValuePair<string, Button> pair in byTopicId)
        {
            if (!usedTopicIds.Contains(pair.Key))
                pair.Value.gameObject.SetActive(false);
        }

        Debug.Log("Guide: сценовых чипов привязано: " + bound);
        return bound > 0;
    }

    private Dictionary<string, Button> CollectSceneChipsByTopicId()
    {
        var map = new Dictionary<string, Button>();

        foreach (Transform child in chipsContainer)
        {
            if (child.name.Contains("ChipTemplate"))
                continue;

            if (!TryParseChipTopicId(child.name, out string topicId))
                continue;

            Button button = child.GetComponent<Button>();
            if (button != null)
                map[topicId] = button;
        }

        return map;
    }

    private static bool TryParseChipTopicId(string chipObjectName, out string topicId)
    {
        const string prefix = "Chip_";
        if (chipObjectName.StartsWith(prefix, System.StringComparison.Ordinal))
        {
            topicId = chipObjectName.Substring(prefix.Length);
            return !string.IsNullOrEmpty(topicId);
        }

        topicId = null;
        return false;
    }

    private IEnumerator RefreshChipLabelsAfterLayout()
    {
        yield return null;

        if (chipsContainer is RectTransform chipsRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(chipsRect);

        Canvas.ForceUpdateCanvases();

        foreach (Transform child in chipsContainer)
        {
            if (!TryParseChipTopicId(child.name, out _))
                continue;

            TMP_Text text = child.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.ForceMeshUpdate(true);
        }
    }

    private Button SpawnChipButton(GuideSuggestionItem item)
    {
        GameObject prefab = chipPrefabAsset;
        if (prefab != null)
        {
            GameObject clone = Instantiate(prefab, chipsContainer);
            clone.SetActive(true);
            ApplyChipLabel(clone, item.label);
            return clone.GetComponent<Button>();
        }

        if (chipButtonPrefab != null && IsPrefabAsset(chipButtonPrefab.gameObject))
        {
            GameObject clone = Instantiate(chipButtonPrefab.gameObject, chipsContainer);
            clone.SetActive(true);
            ApplyChipLabel(clone, item.label);
            return clone.GetComponent<Button>();
        }

        return CreateChipButtonRuntime(item);
    }

    private static bool IsPrefabAsset(GameObject go)
    {
        return go != null && string.IsNullOrEmpty(go.scene.name);
    }

    private static void ApplyChipLabel(GameObject chipRoot, string label)
    {
        TMP_Text text = chipRoot.GetComponentInChildren<TMP_Text>(true);
        if (text == null || string.IsNullOrEmpty(label))
            return;

        if (text.text == label)
            return;

        text.text = label;
        text.ForceMeshUpdate(true);
    }

    private Button CreateChipButtonRuntime(GuideSuggestionItem item)
    {
        GameObject go = new GameObject(
            "Chip_" + item.topicId,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(chipsContainer, false);
        rt.sizeDelta = chipCellSize;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.93f, 0.93f, 0.93f, 0.95f);
        image.raycastTarget = true;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.SetParent(rt, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(8f, 4f);
        labelRt.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text = item.label;
        tmp.fontSize = 20f;
        tmp.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        if (welcomeText != null)
            tmp.font = welcomeText.font;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        go.SetActive(true);
        return button;
    }

    private void SetupChipsGrid(int chipCount)
    {
        if (chipsContainer == null)
            return;

        GameObject chipsGo = chipsContainer.gameObject;

        VerticalLayoutGroup vertical = chipsGo.GetComponent<VerticalLayoutGroup>();
        if (vertical != null)
            vertical.enabled = false;

        GridLayoutGroup grid = chipsGo.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = chipsGo.AddComponent<GridLayoutGroup>();
        if (grid == null)
            return;

        grid.enabled = true;

        // Кнопки настроены в сцене — не затираем Cell Size / Spacing при Play.
        if (useSceneChips && HasDesignerGridLayout(grid))
        {
            if (chipsContainer is RectTransform chipsRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(chipsRect);
            return;
        }

        Vector2 cell = chipCellSize;
        if (chipButtonPrefab != null)
        {
            RectTransform prefabRect = chipButtonPrefab.GetComponent<RectTransform>();
            if (prefabRect != null && prefabRect.sizeDelta.x > 10f && prefabRect.sizeDelta.y > 10f)
                cell = prefabRect.sizeDelta;
        }

        int columns = Mathf.Max(1, chipColumns);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.cellSize = cell;
        grid.spacing = chipSpacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;

        if (chipsContainer is RectTransform chipsRectRuntime)
        {
            int rows = Mathf.Max(1, Mathf.CeilToInt(chipCount / (float)columns));
            float width = columns * cell.x + (columns - 1) * chipSpacing.x;
            float height = rows * cell.y + (rows - 1) * chipSpacing.y;
            chipsRectRuntime.sizeDelta = new Vector2(width, height);
        }
    }

    private static bool HasDesignerGridLayout(GridLayoutGroup grid)
    {
        return grid.cellSize.x > 10f && grid.cellSize.y > 10f;
    }

    private void OnSendClicked()
    {
        if (messageInput == null)
            return;

        string text = messageInput.text?.Trim();
        if (string.IsNullOrEmpty(text))
            return;

        messageInput.text = "";
        StartCoroutine(SendUserMessage(text));
    }

    private IEnumerator SendTopic(string topicId, string labelForLog)
    {
        CloseLeadingQuestionsPanel();
        HideWelcomeText();
        followLatestMessages = true;
        AppendLine("Вы", string.IsNullOrEmpty(labelForLog) ? topicId : labelForLog);

        if (!serverOnline && TryOfflineTopic(topicId, out string offlineReply))
        {
            AppendLine("Мартишка", offlineReply);
            yield break;
        }

        GuideChatTopicRequest body = new GuideChatTopicRequest { topicId = topicId };
        yield return PostChat(JsonUtility.ToJson(body));
    }

    private IEnumerator SendUserMessage(string message)
    {
        HideWelcomeText();
        followLatestMessages = true;
        AppendLine("Вы", message);

        if (!serverOnline && TryOfflineMessage(message, out string offlineReply))
        {
            AppendLine("Мартишка", offlineReply);
            yield break;
        }

        GuideChatMessageRequest body = new GuideChatMessageRequest { message = message };
        yield return PostChat(JsonUtility.ToJson(body));
    }

    private IEnumerator PostChat(string jsonBody)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using UnityWebRequest request = new UnityWebRequest(ChatUrl, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            if (useOfflineFallback && TryOfflineMessageFromJsonBody(jsonBody, out string fallback))
            {
                AppendLine("Мартишка", fallback);
                yield break;
            }

            AppendLine("Мартишка", "Ошибка связи: " + request.error);
            yield break;
        }

        GuideChatResponseDto response =
            JsonUtility.FromJson<GuideChatResponseDto>(request.downloadHandler.text);

        if (response != null && !string.IsNullOrEmpty(response.reply))
        {
            if (response.source == "no_match"
                && useOfflineFallback
                && TryOfflineMessageFromJsonBody(jsonBody, out string offlineReply)
                && offlineReply != offlineData?.noMatch)
            {
                AppendLine("Мартишка", offlineReply);
            }
            else
            {
                AppendLine("Мартишка", response.reply);
            }
        }
        else
            AppendLine("Мартишка", "Пустой ответ сервера.");
    }

    private bool TryOfflineMessageFromJsonBody(string jsonBody, out string reply)
    {
        reply = offlineData?.noMatch;
        if (offlineData == null || string.IsNullOrEmpty(jsonBody))
            return false;

        if (jsonBody.Contains("topicId"))
        {
            GuideChatTopicRequest req = JsonUtility.FromJson<GuideChatTopicRequest>(jsonBody);
            return TryOfflineTopic(req?.topicId, out reply);
        }

        GuideChatMessageRequest msgReq = JsonUtility.FromJson<GuideChatMessageRequest>(jsonBody);
        return TryOfflineMessage(msgReq?.message, out reply);
    }

    private bool TryOfflineTopic(string topicId, out string reply)
    {
        reply = null;
        if (offlineData?.topics == null)
            return false;

        foreach (GuideTopic topic in offlineData.topics)
        {
            if (topic != null && topic.id == topicId)
            {
                reply = topic.answer;
                return true;
            }
        }

        return false;
    }

    private bool TryOfflineMessage(string message, out string reply)
    {
        reply = offlineData?.noMatch;
        if (offlineData?.topics == null)
            return false;

        string norm = Normalize(message);
        if (IsOffTopic(norm))
        {
            reply = offlineData.offTopic;
            return true;
        }

        List<string> variants = GuideMessageVariants(norm);
        GuideTopic best = null;
        int bestScore = 0;

        foreach (GuideTopic topic in offlineData.topics)
        {
            if (topic?.keywords == null || topic.keywords.Length == 0)
                continue;

            int score = 0;
            foreach (string variant in variants)
            {
                foreach (string kw in topic.keywords)
                {
                    string k = Normalize(kw);
                    if (!string.IsNullOrEmpty(k) && variant.Contains(k))
                        score += Mathf.Max(k.Length, 3);
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = topic;
            }
        }

        if (best != null && bestScore > 0)
        {
            reply = best.answer;
            return true;
        }

        return offlineData.noMatch != null;
    }

    private static List<string> GuideMessageVariants(string messageNorm)
    {
        var variants = new List<string> { messageNorm };
        if (string.IsNullOrEmpty(messageNorm))
            return variants;

        string stripped = messageNorm;
        stripped = System.Text.RegularExpressions.Regex.Replace(
            stripped,
            @"^(расскажи|подскажи|объясни|скажи|расскажите|подскажите)(\s+мне)?(\s+про)?\s+",
            "");
        stripped = System.Text.RegularExpressions.Regex.Replace(
            stripped,
            @"^(что\s+такое|что\s+значит|что\s+это|кто\s+такой|кто\s+такая|какой\s+это|какая\s+это)\s+",
            "");
        stripped = System.Text.RegularExpressions.Regex.Replace(
            stripped,
            @"^(как|где|когда|почему|зачем|сколько)\s+",
            "");
        stripped = stripped.Trim();

        if (!string.IsNullOrEmpty(stripped) && stripped != messageNorm)
            variants.Add(stripped);

        return variants;
    }

    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";

        s = s.ToLowerInvariant();
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            if (char.IsLetterOrDigit(c) || c == '\'')
                sb.Append(c);
            else
                sb.Append(' ');
        }

        return sb.ToString().Replace("  ", " ").Trim();
    }

    private static bool IsOffTopic(string norm)
    {
        string[] words =
        {
            "погод", "рецепт", "футбол", "политик", "крипт", "свидан", "диет", "лечен", "врач"
        };

        foreach (string w in words)
        {
            if (norm.Contains(w))
                return true;
        }

        return false;
    }

    private const string UserAuthor = "Вы";
    private const string GuideAuthor = "Мартишка";

    private void AppendLine(string author, string text)
    {
        if (chatLogText == null)
        {
            Debug.Log($"[{author}] {text}");
            return;
        }

        string line = FormatChatLine(author, text);
        if (string.IsNullOrEmpty(chatLogText.text))
            chatLogText.text = line;
        else
            chatLogText.text += "\n\n" + line;

        chatLogText.ForceMeshUpdate(true);
        SyncDialogueScrollContentSize();

        if (ShouldAutoScrollAfterMessage(author))
            RequestScrollToBottom();
    }

    private static string FormatChatLine(string author, string text)
    {
        string align = author == UserAuthor ? "right" : "left";
        string safeText = EscapeTmpRichText(text);
        return $"<align=\"{align}\"><b>{author}:</b> {safeText}</align>";
    }

    private static string EscapeTmpRichText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
