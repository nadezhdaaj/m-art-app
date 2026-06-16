using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Категории заметок. Цвет плашки = смысл заметки (идея 1).
/// Порядок в <see cref="Ordered"/> совпадает с порядком кружков-свотчей на экране.
/// </summary>
public enum NoteCategory
{
    Idea = 0,
    Liked = 1,
    Question = 2,
    Important = 3,
    Todo = 4,
}

public static class NoteCategories
{
    /// <summary>
    /// Порядок кружков слева направо (как на экране заметок):
    /// 1=Идея, 2=Важное, 3=Вопрос, 4=Понравилось, 5=To-do.
    /// </summary>
    public static readonly NoteCategory[] Ordered =
    {
        NoteCategory.Idea,
        NoteCategory.Important,
        NoteCategory.Question,
        NoteCategory.Liked,
        NoteCategory.Todo,
    };

    public static NoteCategory Default => NoteCategory.Idea;

    // Цвета, считанные из кружков пользователя в рантайме (чтобы карточки и фильтр
    // совпадали с его палитрой, а не с дефолтной).
    private static readonly Dictionary<NoteCategory, Color> colorOverrides = new Dictionary<NoteCategory, Color>();

    public static void SetColorOverride(NoteCategory category, Color color)
    {
        colorOverrides[category] = color;
    }

    /// <summary>Ключ для бэкенда (совпадает с NOTE_CATEGORIES в index.js).</summary>
    public static string Key(NoteCategory category)
    {
        switch (category)
        {
            case NoteCategory.Idea: return "idea";
            case NoteCategory.Liked: return "liked";
            case NoteCategory.Question: return "question";
            case NoteCategory.Important: return "important";
            case NoteCategory.Todo: return "todo";
            default: return "idea";
        }
    }

    public static NoteCategory FromKey(string key)
    {
        switch ((key ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "liked": return NoteCategory.Liked;
            case "question": return NoteCategory.Question;
            case "important": return NoteCategory.Important;
            case "todo": return NoteCategory.Todo;
            default: return NoteCategory.Idea;
        }
    }

    /// <summary>Цвет плашки. Сначала берём цвет кружка пользователя (если считан), иначе дефолт.</summary>
    public static Color GetColor(NoteCategory category)
    {
        if (colorOverrides.TryGetValue(category, out Color overrideColor))
        {
            return overrideColor;
        }

        switch (category)
        {
            case NoteCategory.Idea: return new Color32(0xFF, 0xD1, 0x6A, 0xFF);      // жёлтый
            case NoteCategory.Liked: return new Color32(0xF4, 0x8F, 0xB1, 0xFF);     // розовый
            case NoteCategory.Question: return new Color32(0x6E, 0xC6, 0xF0, 0xFF);  // голубой
            case NoteCategory.Important: return new Color32(0xB3, 0x8B, 0xE6, 0xFF); // фиолетовый
            case NoteCategory.Todo: return new Color32(0x9C, 0xD6, 0x7E, 0xFF);      // зелёный
            default: return UnityEngine.Color.white;
        }
    }

    public static string Emoji(NoteCategory category)
    {
        switch (category)
        {
            case NoteCategory.Idea: return "\U0001F4A1";       // 💡
            case NoteCategory.Liked: return "❤";          // ❤
            case NoteCategory.Question: return "❓";       // ❓
            case NoteCategory.Important: return "⭐";      // ⭐
            case NoteCategory.Todo: return "\U0001F4CC";       // 📌
            default: return string.Empty;
        }
    }

    public static string Label(NoteCategory category)
    {
        switch (category)
        {
            case NoteCategory.Idea: return "Идея";
            case NoteCategory.Liked: return "Понравилось";
            case NoteCategory.Question: return "Вопрос";
            case NoteCategory.Important: return "Важное";
            case NoteCategory.Todo: return "To-do";
            default: return string.Empty;
        }
    }

    /// <summary>Подпись с эмодзи: «💡 Идея».</summary>
    public static string EmojiLabel(NoteCategory category)
    {
        return Emoji(category) + " " + Label(category);
    }
}
