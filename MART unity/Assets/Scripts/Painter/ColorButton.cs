using UnityEngine;
using UnityEngine.UI;

public class ColorButton : MonoBehaviour
{
    public PaletteManager paletteManager;
    public Painter painter;

    public int colorIndex = 0;
    public Color color = Color.white;

    public Image buttonImage;

    private void Awake()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
    }

    private void Start()
    {
        if (paletteManager == null)
            paletteManager = FindObjectOfType<PaletteManager>(true);

        if (painter == null)
            painter = FindObjectOfType<Painter>(true);

        UpdateColorFromIndex();
    }

    private void OnValidate()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        UpdateColorFromIndex();
    }

    public void UpdateColorFromIndex()
    {
        if (paletteManager == null)
            paletteManager = FindObjectOfType<PaletteManager>(true);

        if (paletteManager == null)
            return;

        if (colorIndex >= 0 && colorIndex < paletteManager.colors.Length)
        {
            color = paletteManager.colors[colorIndex];
        }

        if (buttonImage != null)
            buttonImage.color = color;
    }


    public void ApplyColor()
    {
        if (paletteManager == null)
            paletteManager = FindObjectOfType<PaletteManager>(true);

        if (paletteManager != null && colorIndex >= 0 && colorIndex < paletteManager.colors.Length)
            color = paletteManager.colors[colorIndex];

        if (paletteManager != null && paletteManager.painter != null)
            painter = paletteManager.painter;

        if (painter == null)
            painter = FindObjectOfType<Painter>(true);

        if (painter == null)
        {
            Debug.LogWarning("ColorButton: Painter not found.");
            return;
        }

        painter.SetColor(color);

        Debug.Log($"Color applied: RGB({color.r}, {color.g}, {color.b}) index={colorIndex}");
    }
}
