using UnityEngine;

public class PaletteManager : MonoBehaviour
{
    public Painter painter;

    public Color[] colors = new Color[15]
    {
        Color.black,
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        new Color(1f, 0.5f, 0f),      // Orange
        Color.magenta,
        Color.cyan,
        Color.gray,
        new Color(0.5f, 0f, 0.5f),   // Purple
        new Color(0.65f, 0.16f, 0.16f), // Brown
        new Color(0.12f, 0.56f, 1f),   // Light Blue
        new Color(0.55f, 0.27f, 0.07f), // Dark Orange
        new Color(0f, 0.5f, 0f),       // Dark Green
        new Color(1f, 0.75f, 0.8f)     // Pink
    };

    private void Start()
    {
        RefreshPainterReference();
        SetupButtons();
    }

    private void SetupButtons()
    {
        RefreshPainterReference();

        ColorButton[] buttons = FindObjectsOfType<ColorButton>(true);

        foreach (ColorButton button in buttons)
        {
            button.paletteManager = this;
            button.painter = painter;

            int parsedIndex;
            if (int.TryParse(button.gameObject.name, out parsedIndex))
            {
                int targetIndex = parsedIndex - 1;
                if (targetIndex >= 0 && targetIndex < colors.Length)
                {
                    button.colorIndex = targetIndex;
                }
            }

            button.UpdateColorFromIndex();
        }
    }

    public void SelectColor(int index)
    {
        RefreshPainterReference();

        if (index >= 0 && index < colors.Length && painter != null)
        {
            painter.SetColor(colors[index]);
            Debug.Log($"PaletteManager: Selected color {index} - RGB({colors[index].r}, {colors[index].g}, {colors[index].b})");
        }
        else if (index < 0 || index >= colors.Length)
        {
            Debug.LogWarning($"PaletteManager: invalid select color index {index}");
        }
        else if (painter == null)
        {
            Debug.LogWarning("PaletteManager: Painter is not assigned or found.");
        }
    }

    private void RefreshPainterReference()
    {
        if (painter == null)
            painter = FindObjectOfType<Painter>(true);
    }
}
