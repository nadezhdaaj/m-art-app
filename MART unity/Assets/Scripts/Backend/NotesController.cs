using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Заметки пользователя: цвет плашки = категория (идея 1) + фильтр сверху.
/// Повесь на экран заметок и назначь ссылки в инспекторе (см. поля ниже).
/// Список карточек и чипсы фильтра строятся в рантайме.
/// </summary>
[DisallowMultipleComponent]
public class NotesController : MonoBehaviour
{
    [Header("Ввод заметки (назначь свои объекты)")]
    [SerializeField] private TMP_InputField noteInput;
    [SerializeField] private Button saveButton;
    [Tooltip("Родитель 5 кружков-цветов. Порядок слева направо = Идея, Важное, Вопрос, Понравилось, To-do.")]
    [SerializeField] private Transform colorSwatchesRoot;

    [Header("Назад в профиль (OtherPanel)")]
    [Tooltip("Кнопка Back. Если не назначить — найдётся по имени/подписи (back/назад).")]
    [SerializeField] private Button backButton;
    [Tooltip("Что скрывать при нажатии Back. По умолчанию — объект с этим компонентом.")]
    [SerializeField] private GameObject notesScreenRoot;

    [Header("Подписи категории сверху (показывается выбранная, остальные скрыты)")]
    [Tooltip("По желанию назначь объекты-подписи в порядке: Идея, Важное, Вопрос, Понравилось, To-do.\n" +
             "Если не назначишь — найдутся автоматически по имени (идея/важное/вопрос/понравилось/todo).")]
    [SerializeField] private GameObject[] categoryIndicators;

    [Header("Список и фильтр (контейнеры; разметка строится сама)")]
    [SerializeField] private Transform notesListRoot;
    [SerializeField] private Transform filterBarRoot;
    [SerializeField] private TMP_Text statusText;

    private NoteCategory currentCategory = NoteCategory.Idea;
    private bool hasCategorySelected;
    private bool hasFilter;
    private NoteCategory filterCategory;
    private bool isBusy;
    private string editingNoteId;

    private readonly Dictionary<NoteCategory, GameObject> indicators = new Dictionary<NoteCategory, GameObject>();

    private readonly List<Image> swatchImages = new List<Image>();
    private readonly List<NoteCategory> swatchCategories = new List<NoteCategory>();
    private readonly List<Button> filterButtons = new List<Button>();
    private readonly List<Image> filterButtonImages = new List<Image>();

    private void OnEnable()
    {
        ResolveReferences();
        EnsureRuntimeContainers();
        SetupSwatches();
        ResolveIndicators();
        SetupFilterBar();
        EnsureListLayout();

        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(OnSaveClicked);
            saveButton.onClick.AddListener(OnSaveClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
            backButton.onClick.AddListener(OnBackClicked);
        }

        // Изначально ничего не выбрано: все подписи скрыты, кружки не подсвечены.
        hasCategorySelected = false;
        UpdateSwatchHighlight();
        UpdateIndicators();

        LoadNotes();
    }

    private void ResolveReferences()
    {
        if (noteInput == null)
        {
            noteInput = ResolveNoteInput();
        }

        if (saveButton == null)
        {
            saveButton = FindButtonByLabel("сохран", "save");
        }

        if (colorSwatchesRoot == null)
        {
            colorSwatchesRoot = FindSwatchesRoot();
        }

        if (backButton == null)
        {
            backButton = FindButtonByLabel("back", "назад");
        }
    }

    /// <summary>Поле ввода заметки: сначала по имени (замет/добав), иначе первое TMP-поле.</summary>
    private TMP_InputField ResolveNoteInput()
    {
        TMP_InputField[] fields = GetComponentsInChildren<TMP_InputField>(true);
        for (int i = 0; i < fields.Length; i++)
        {
            string name = fields[i].name.ToLowerInvariant();
            if (name.Contains("замет") || name.Contains("добав") || name.Contains("note"))
            {
                return fields[i];
            }
        }

        return fields.Length > 0 ? fields[0] : null;
    }

    /// <summary>Ищет кнопку по имени объекта или тексту подписи (TMP/Text).</summary>
    private Button FindButtonByLabel(params string[] keywords)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            string haystack = button.name;
            TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                haystack += " " + tmp.text;
            }
            Text legacy = button.GetComponentInChildren<Text>(true);
            if (legacy != null)
            {
                haystack += " " + legacy.text;
            }

            haystack = haystack.ToLowerInvariant();
            for (int k = 0; k < keywords.Length; k++)
            {
                if (haystack.Contains(keywords[k]))
                {
                    return button;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Ищет контейнер кружков: сначала по имени (цвет/color/swatch/палитра/picker),
    /// затем — первый объект с 5+ дочерними Image (ряд свотчей).
    /// </summary>
    private Transform FindSwatchesRoot()
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < all.Length; i++)
        {
            string name = all[i].name.ToLowerInvariant();
            if (all[i] != transform &&
                (name.Contains("цвет") || name.Contains("color") || name.Contains("swatch") ||
                 name.Contains("палитр") || name.Contains("picker")) &&
                CountImageChildren(all[i]) >= 3)
            {
                return all[i];
            }
        }

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == transform)
            {
                continue;
            }

            int imageChildren = CountImageChildren(all[i]);
            if (imageChildren >= 5 && imageChildren <= 8)
            {
                return all[i];
            }
        }

        return null;
    }

    private static int CountImageChildren(Transform parent)
    {
        int count = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).GetComponent<Image>() != null)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Если контейнер списка не назначен — строим прокручиваемую панель со списком
    /// и полосой фильтра в нижней половине экрана. Потом можно переназначить свои объекты.
    /// </summary>
    private void EnsureRuntimeContainers()
    {
        if (notesListRoot != null)
        {
            return;
        }

        RectTransform panelRect = BuildContainers();

        // Поставить панель ровно под кружки цветов (после раскладки), даже если они в другом контейнере.
        if (panelRect != null && colorSwatchesRoot is RectTransform swatchRect)
        {
            StartCoroutine(PositionListUnderSwatches(panelRect, swatchRect));
        }
    }

    /// <summary>
    /// Строит панель списка (NotesRuntimePanel: FilterBar + Scroll/Viewport/Content) и назначает
    /// notesListRoot/filterBarRoot. Работает и в редакторе (без корутин/Destroy), чтобы объекты
    /// были видны в иерархии и сохранялись в сцену. Возвращает RectTransform панели.
    /// </summary>
    /// <summary>Уже ли построен (или назначен) контейнер списка.</summary>
    public bool HasListContainer => notesListRoot != null;

    public RectTransform BuildContainers()
    {
        if (notesListRoot != null)
        {
            return notesListRoot as RectTransform;
        }

        GameObject panel = new GameObject("NotesRuntimePanel", typeof(RectTransform));
        // Родитель списка: плашка «дополнение» (по имени), иначе контейнер цветов, иначе сама панель.
        Transform dopPanel = FindDescendantContaining("дополн");
        Transform listParent = dopPanel != null
            ? dopPanel
            : (colorSwatchesRoot != null && colorSwatchesRoot.parent != null ? colorSwatchesRoot.parent : transform);
        panel.transform.SetParent(listParent, false);
        panel.transform.SetAsLastSibling();
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        // По умолчанию — средняя полоса плашки: под цветами (сверху), над «Сохранить» (снизу).
        panelRect.anchorMin = new Vector2(0.05f, 0.16f);
        panelRect.anchorMax = new Vector2(0.95f, 0.78f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject filterGO = new GameObject("FilterBar", typeof(RectTransform));
        filterGO.transform.SetParent(panel.transform, false);
        RectTransform filterRect = filterGO.GetComponent<RectTransform>();
        filterRect.anchorMin = new Vector2(0f, 1f);
        filterRect.anchorMax = new Vector2(1f, 1f);
        filterRect.pivot = new Vector2(0.5f, 1f);
        filterRect.sizeDelta = new Vector2(0f, 80f);
        filterRect.anchoredPosition = Vector2.zero;
        filterBarRoot = filterRect;

        GameObject scrollGO = new GameObject("NotesScroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollGO.transform.SetParent(panel.transform, false);
        RectTransform scrollRect = scrollGO.GetComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = new Vector2(0f, -88f);
        scrollGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);

        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        viewportGO.transform.SetParent(scrollGO.transform, false);
        RectTransform viewportRect = viewportGO.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportRect.pivot = new Vector2(0.5f, 1f);
        viewportGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

        GameObject contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        notesListRoot = contentRect;

        ScrollRect sr = scrollGO.GetComponent<ScrollRect>();
        sr.viewport = viewportRect;
        sr.content = contentRect;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        EnsureListLayout();
        return panelRect;
    }

    /// <summary>Ищет потомка, в имени которого есть фрагмент (включая выключенные).</summary>
    private Transform FindDescendantContaining(string fragment)
    {
        string needle = fragment.ToLowerInvariant();
        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != transform && all[i].name.ToLowerInvariant().Contains(needle))
            {
                return all[i];
            }
        }

        return null;
    }

    /// <summary>Ставит превью-панель ровно под ряд кружков: та же ширина, сразу под ними до низа контейнера.</summary>
    private IEnumerator PositionListUnderSwatches(RectTransform panel, RectTransform swatches)
    {
        yield return null; // дождаться раскладки UI
        Canvas.ForceUpdateCanvases();

        if (panel == null || swatches == null)
        {
            yield break;
        }

        RectTransform parent = panel.parent as RectTransform;
        if (parent == null)
        {
            yield break;
        }

        Rect parentRect = parent.rect;
        if (parentRect.width <= 1f || parentRect.height <= 1f)
        {
            yield break;
        }

        Vector3[] corners = new Vector3[4];
        swatches.GetWorldCorners(corners); // 0=BL, 1=TL, 2=TR, 3=BR
        Vector2 bottomLeft = parent.InverseTransformPoint(corners[0]);

        // Ширина — на всю плашку (а не по кружкам), подгоняем только верх — под низ кружков.
        float swatchesBottom = Mathf.Clamp01((bottomLeft.y - parentRect.yMin) / parentRect.height);

        const float gap = 0.02f;
        float yMax = Mathf.Clamp01(swatchesBottom - gap);
        float yMin = 0.14f; // оставляем место снизу под кнопку «Сохранить»
        if (yMax <= yMin)
        {
            yMin = Mathf.Max(0f, yMax - 0.4f);
        }

        panel.anchorMin = new Vector2(0.04f, yMin);
        panel.anchorMax = new Vector2(0.96f, yMax);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
    }

    // ---------- Свотчи цвета = выбор категории ----------

    private void SetupSwatches()
    {
        swatchImages.Clear();
        swatchCategories.Clear();

        if (colorSwatchesRoot == null)
        {
            return;
        }

        int index = 0;
        for (int i = 0; i < colorSwatchesRoot.childCount && index < NoteCategories.Ordered.Length; i++)
        {
            Transform child = colorSwatchesRoot.GetChild(i);
            Image image = child.GetComponent<Image>();
            if (image == null)
            {
                continue;
            }

            NoteCategory category = NoteCategories.Ordered[index];

            // Не трогаем цвет кружка пользователя — наоборот, берём его как цвет категории,
            // чтобы карточки и фильтр совпадали с его палитрой.
            NoteCategories.SetColorOverride(category, image.color);

            Button button = child.GetComponent<Button>();
            if (button == null)
            {
                button = child.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = image;
            }

            NoteCategory captured = category;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectCategory(captured));

            swatchImages.Add(image);
            swatchCategories.Add(category);
            index++;
        }
    }

    private void SelectCategory(NoteCategory category)
    {
        currentCategory = category;
        hasCategorySelected = true;
        UpdateSwatchHighlight();
        UpdateIndicators();
    }

    private void UpdateSwatchHighlight()
    {
        for (int i = 0; i < swatchImages.Count; i++)
        {
            bool selected = hasCategorySelected && swatchCategories[i] == currentCategory;
            float scale = selected ? 1.25f : 1f;
            swatchImages[i].rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    // ---------- Подписи категории сверху ----------

    private void ResolveIndicators()
    {
        indicators.Clear();

        // 1) Назначенные вручную объекты в порядке Ordered.
        if (categoryIndicators != null)
        {
            for (int i = 0; i < categoryIndicators.Length && i < NoteCategories.Ordered.Length; i++)
            {
                if (categoryIndicators[i] != null)
                {
                    indicators[NoteCategories.Ordered[i]] = categoryIndicators[i];
                }
            }
        }

        // 2) Для недостающих — автопоиск по имени объекта.
        for (int i = 0; i < NoteCategories.Ordered.Length; i++)
        {
            NoteCategory category = NoteCategories.Ordered[i];
            if (indicators.ContainsKey(category))
            {
                continue;
            }

            GameObject found = FindIndicator(category);
            if (found != null)
            {
                indicators[category] = found;
            }
        }
    }

    private GameObject FindIndicator(NoteCategory category)
    {
        string[] keywords = IndicatorKeywords(category);
        Transform[] all = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == transform)
            {
                continue;
            }

            string name = all[i].name.ToLowerInvariant();
            for (int k = 0; k < keywords.Length; k++)
            {
                if (name.Contains(keywords[k]))
                {
                    return all[i].gameObject;
                }
            }
        }

        return null;
    }

    private static string[] IndicatorKeywords(NoteCategory category)
    {
        switch (category)
        {
            case NoteCategory.Idea: return new[] { "идея", "idea" };
            case NoteCategory.Important: return new[] { "важн", "important" };
            case NoteCategory.Question: return new[] { "вопрос", "question" };
            case NoteCategory.Liked: return new[] { "понрав", "нрав", "liked", "like" };
            case NoteCategory.Todo: return new[] { "todo", "туду", "дело" };
            default: return new string[0];
        }
    }

    private void UpdateIndicators()
    {
        foreach (KeyValuePair<NoteCategory, GameObject> pair in indicators)
        {
            if (pair.Value != null)
            {
                pair.Value.SetActive(hasCategorySelected && pair.Key == currentCategory);
            }
        }
    }

    // ---------- Фильтр по категории ----------

    private void SetupFilterBar()
    {
        if (filterBarRoot == null)
        {
            return;
        }

        for (int i = filterBarRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(filterBarRoot.GetChild(i).gameObject);
        }

        filterButtons.Clear();
        filterButtonImages.Clear();

        EnsureHorizontalLayout(filterBarRoot);

        CreateFilterChip("Все", false, NoteCategory.Idea);
        for (int i = 0; i < NoteCategories.Ordered.Length; i++)
        {
            NoteCategory category = NoteCategories.Ordered[i];
            CreateFilterChip(NoteCategories.Label(category), true, category);
        }

        UpdateFilterHighlight();
    }

    private void CreateFilterChip(string label, bool isCategory, NoteCategory category)
    {
        GameObject chip = new GameObject(isCategory ? "Filter_" + NoteCategories.Key(category) : "Filter_All",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        chip.transform.SetParent(filterBarRoot, false);

        LayoutElement element = chip.AddComponent<LayoutElement>();
        element.minHeight = 64f;
        element.preferredHeight = 64f;
        // Без явной ширины чипсы схлопываются под HorizontalLayoutGroup.
        float chipWidth = Mathf.Max(96f, label.Length * 18f + 44f);
        element.minWidth = chipWidth;
        element.preferredWidth = chipWidth;

        Image image = chip.GetComponent<Image>();
        image.color = isCategory ? NoteCategories.GetColor(category) : new Color(0.8f, 0.8f, 0.8f, 1f);

        Button button = chip.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = image;

        if (isCategory)
        {
            NoteCategory captured = category;
            button.onClick.AddListener(() => SetFilter(true, captured));
        }
        else
        {
            button.onClick.AddListener(() => SetFilter(false, NoteCategory.Idea));
        }

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(chip.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20f, 6f);
        labelRect.offsetMax = new Vector2(-20f, -6f);

        TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }
        text.text = label;
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        filterButtons.Add(button);
        filterButtonImages.Add(image);
    }

    private void SetFilter(bool active, NoteCategory category)
    {
        hasFilter = active;
        filterCategory = category;
        UpdateFilterHighlight();
        ApplyFilter();
    }

    private void UpdateFilterHighlight()
    {
        // index 0 = «Все», далее по Ordered.
        for (int i = 0; i < filterButtonImages.Count; i++)
        {
            bool isAllChip = i == 0;
            bool selected = isAllChip ? !hasFilter
                : hasFilter && NoteCategories.Ordered[i - 1] == filterCategory;

            Color baseColor = filterButtonImages[i].color;
            baseColor.a = selected ? 1f : 0.45f;
            filterButtonImages[i].color = baseColor;

            float scale = selected ? 1.08f : 1f;
            filterButtonImages[i].rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void ApplyFilter()
    {
        if (notesListRoot == null)
        {
            return;
        }

        for (int i = 0; i < notesListRoot.childCount; i++)
        {
            NoteCardView card = notesListRoot.GetChild(i).GetComponent<NoteCardView>();
            if (card == null)
            {
                continue;
            }

            bool visible = !hasFilter || card.Category == filterCategory;
            card.gameObject.SetActive(visible);
        }
    }

    // ---------- Сохранение ----------

    private void OnSaveClicked()
    {
        if (isBusy)
        {
            return;
        }

        if (BackendManager.instance == null)
        {
            ShowPopup("Войдите в аккаунт, чтобы сохранять заметки.");
            return;
        }

        if (!hasCategorySelected)
        {
            ShowPopup("Выберите цвет заметки.");
            return;
        }

        string text = noteInput != null ? noteInput.text?.Trim() : null;
        if (string.IsNullOrWhiteSpace(text))
        {
            ShowPopup("Введите текст заметки.");
            return;
        }

        isBusy = true;
        if (saveButton != null)
        {
            saveButton.interactable = false;
        }

        bool isEditing = !string.IsNullOrWhiteSpace(editingNoteId);
        ShowStatus(isEditing ? "Обновляем заметку..." : "Сохраняем заметку...");

        if (isEditing)
        {
            BackendManager.instance.UpdateNote(editingNoteId, text, currentCategory, HandleNoteSaved);
        }
        else
        {
            BackendManager.instance.SaveNote(text, currentCategory, string.Empty, HandleNoteSaved);
        }
    }

    private void HandleNoteSaved(ApiResult<NoteDto> result)
    {
        isBusy = false;
        if (saveButton != null)
        {
            saveButton.interactable = true;
        }

        if (result == null || !result.Success || result.Data == null)
        {
            string message = result == null || string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? "Не удалось сохранить заметку."
                : result.ErrorMessage;
            ShowStatus(message);
            ShowPopup(message);
            return;
        }

        bool wasEditing = !string.IsNullOrWhiteSpace(editingNoteId);
        editingNoteId = null;

        if (noteInput != null)
        {
            noteInput.text = string.Empty;
        }

        ShowStatus(string.Empty);
        ShowPopup(wasEditing ? "Заметка обновлена" : "Успешно сохранено");

        // Перезагружаем список, чтобы превью обновилось/добавилось.
        LoadNotes();
    }

    // ---------- Всплывающее уведомление (самодостаточное, без внешних объектов) ----------

    private void ShowPopup(string message)
    {
        ToastNotification.Show(message); // если в сцене есть NotificationText — покажется и там

        Transform parent = ResolvePopupCanvas();
        if (parent == null)
        {
            return;
        }

        GameObject popup = new GameObject("NotesPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        popup.transform.SetParent(parent, false);
        popup.transform.SetAsLastSibling();

        RectTransform rect = popup.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(560f, 150f);
        rect.anchoredPosition = new Vector2(0f, 200f);

        Image bg = popup.GetComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        bg.raycastTarget = false;

        GameObject textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(popup.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 16f);
        textRect.offsetMax = new Vector2(-24f, -16f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }
        text.text = message;
        text.fontSize = 34f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.raycastTarget = false;

        StartCoroutine(HidePopup(popup));
    }

    private static IEnumerator HidePopup(GameObject popup)
    {
        yield return new WaitForSeconds(1.6f);
        if (popup != null)
        {
            Destroy(popup);
        }
    }

    private Transform ResolvePopupCanvas()
    {
        Canvas ownCanvas = GetComponentInParent<Canvas>();
        if (ownCanvas != null)
        {
            return ownCanvas.rootCanvas.transform;
        }

        Canvas anyCanvas = FindObjectOfType<Canvas>();
        return anyCanvas != null ? anyCanvas.rootCanvas.transform : null;
    }

    // ---------- Загрузка списка ----------

    private void LoadNotes()
    {
        if (BackendManager.instance == null || notesListRoot == null)
        {
            return;
        }

        ShowStatus("Загружаем заметки...");
        BackendManager.instance.LoadMyNotes(HandleNotesLoaded);
    }

    private void HandleNotesLoaded(ApiResult<NoteArrayWrapperDto> result)
    {
        ClearList();

        if (result == null || !result.Success)
        {
            ShowStatus(result == null || string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? "Не удалось загрузить заметки."
                : result.ErrorMessage);
            return;
        }

        NoteDto[] notes = result.Data != null ? result.Data.items : null;
        if (notes != null)
        {
            for (int i = 0; i < notes.Length; i++)
            {
                NoteCardView.Build(notesListRoot, notes[i], HandleEditCard, HandleDeleteCard);
            }
        }

        ApplyFilter();
        ShowStatus(notes == null || notes.Length == 0 ? "Пока нет заметок." : string.Empty);
    }

    private void HandleEditCard(NoteDto note)
    {
        if (note == null)
        {
            return;
        }

        editingNoteId = note.id;

        if (noteInput != null)
        {
            noteInput.text = note.text ?? string.Empty;
            noteInput.ActivateInputField();
        }

        SelectCategory(NoteCategories.FromKey(note.category));
        ShowStatus("Редактируете заметку. «Сохранить» обновит её.");
    }

    private void HandleDeleteCard(NoteCardView card)
    {
        if (card == null || string.IsNullOrWhiteSpace(card.NoteId) || BackendManager.instance == null)
        {
            return;
        }

        string noteId = card.NoteId;
        BackendManager.instance.DeleteNote(noteId, result =>
        {
            if (result != null && result.Success)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
                ToastNotification.Show("Заметка удалена.");
            }
            else
            {
                ToastNotification.Show("Не удалось удалить заметку.");
            }
        });
    }

    private void ClearList()
    {
        if (notesListRoot == null)
        {
            return;
        }

        for (int i = notesListRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(notesListRoot.GetChild(i).gameObject);
        }
    }

    // ---------- Назад в профиль (OtherPanel) ----------

    private void OnBackClicked()
    {
        // 1) Показать вкладку профиля снизу + экран ProfileUI.
        Navigation nav = FindObjectOfType<Navigation>(true);
        if (nav != null)
        {
            nav.OpenProfile();
        }

        // 2) Внутри ProfileUI показать именно OtherPanel.
        Transform profileRoot = ResolveProfileRoot(nav);
        if (profileRoot != null)
        {
            SetProfilePanelActive(profileRoot, "InformationPanel", false);
            SetProfilePanelActive(profileRoot, "FavouritesPanel", false);
            SetProfilePanelActive(profileRoot, "OtherPanel", true);
        }

        // 3) Скрыть экран заметок.
        GameObject screen = notesScreenRoot != null ? notesScreenRoot : gameObject;
        screen.SetActive(false);
    }

    private static Transform ResolveProfileRoot(Navigation nav)
    {
        if (nav != null && nav.profileScreen != null)
        {
            return nav.profileScreen.transform;
        }

        GameObject profileUi = GameObject.Find("ProfileUI");
        return profileUi != null ? profileUi.transform : null;
    }

    private static void SetProfilePanelActive(Transform profileRoot, string panelName, bool active)
    {
        Transform panel = profileRoot.Find(panelName);
        if (panel == null)
        {
            panel = FindChildByName(profileRoot, panelName);
        }

        if (panel != null)
        {
            panel.gameObject.SetActive(active);
        }
    }

    private static Transform FindChildByName(Transform parent, string targetName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == targetName)
            {
                return child;
            }

            Transform found = FindChildByName(child, targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    // ---------- Разметка контейнеров ----------

    private void EnsureListLayout()
    {
        if (notesListRoot == null)
        {
            return;
        }

        VerticalLayoutGroup layout = notesListRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = notesListRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        layout.spacing = 16f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = notesListRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = notesListRoot.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void EnsureHorizontalLayout(Transform root)
    {
        HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        }
        layout.spacing = 10f;
        layout.padding = new RectOffset(8, 8, 4, 4);
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childAlignment = TextAnchor.MiddleLeft;
    }

    private void ShowStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
