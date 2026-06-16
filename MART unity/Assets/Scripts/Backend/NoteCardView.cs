using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Превью сохранённой заметки: цвет плашки = категория, показывает первую строку.
/// Тап по плашке — открыть заметку на редактирование. Крестик — удалить.
/// </summary>
public class NoteCardView : MonoBehaviour
{
    public string NoteId { get; private set; }
    public NoteCategory Category { get; private set; }
    public NoteDto Note { get; private set; }

    private Action<NoteCardView> onDelete;

    public static NoteCardView Build(Transform parent, NoteDto note, Action<NoteDto> onEdit, Action<NoteCardView> onDeleteCallback)
    {
        NoteCategory category = NoteCategories.FromKey(note != null ? note.category : null);

        GameObject root = new GameObject("NoteCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);

        Image background = root.GetComponent<Image>();
        background.color = NoteCategories.GetColor(category);
        background.raycastTarget = true;

        VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 64, 12, 12); // правый паддинг — место под крестик
        layout.spacing = 2f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        NoteCardView view = root.AddComponent<NoteCardView>();
        view.NoteId = note != null ? note.id : null;
        view.Category = category;
        view.Note = note;
        view.onDelete = onDeleteCallback;

        Button editButton = root.GetComponent<Button>();
        editButton.transition = Selectable.Transition.None;
        editButton.targetGraphic = background;
        if (onEdit != null)
        {
            NoteDto captured = note;
            editButton.onClick.AddListener(() => onEdit(captured));
        }

        CreateText(root.transform, "Category", NoteCategories.Label(category), 22f,
            new Color(0f, 0f, 0f, 0.5f), FontStyles.Bold, TextAlignmentOptions.TopLeft, true);

        TextMeshProUGUI body = CreateText(root.transform, "Body", FirstLine(note != null ? note.text : null), 32f,
            new Color(0.12f, 0.12f, 0.12f, 1f), FontStyles.Normal, TextAlignmentOptions.TopLeft, false);
        body.overflowMode = TextOverflowModes.Ellipsis;

        view.CreateDeleteButton();
        return view;
    }

    private void CreateDeleteButton()
    {
        GameObject buttonObject = new GameObject("DeleteNote",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(transform, false);

        // Игнорируем VerticalLayoutGroup родителя — кнопка висит в углу.
        LayoutElement ignore = buttonObject.AddComponent<LayoutElement>();
        ignore.ignoreLayout = true;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(48f, 48f);
        rect.anchoredPosition = new Vector2(-8f, -8f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.18f);

        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = image;
        button.onClick.AddListener(HandleDeleteClicked);

        TextMeshProUGUI label = CreateText(buttonObject.transform, "X", "×", 34f,
            Color.white, FontStyles.Bold, TextAlignmentOptions.Center, true);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private void HandleDeleteClicked()
    {
        onDelete?.Invoke(this);
    }

    private static TextMeshProUGUI CreateText(
        Transform parent, string name, string content, float fontSize,
        Color color, FontStyles style, TextAlignmentOptions alignment, bool wrap)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = style;
        text.alignment = alignment;
        text.enableWordWrapping = wrap;
        text.raycastTarget = false;
        return text;
    }

    private static string FirstLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "(без текста)";
        }

        int newline = text.IndexOfAny(new[] { '\n', '\r' });
        string line = newline >= 0 ? text.Substring(0, newline) : text;
        return line.Trim();
    }
}
