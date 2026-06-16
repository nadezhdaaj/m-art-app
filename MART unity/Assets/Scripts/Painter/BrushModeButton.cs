using UnityEngine;

public class BrushModeButton : MonoBehaviour
{
    public Painter painter;
    public Painter.BrushType brushType;
    public GameObject closePanelAfterSelect;

    private void Awake()
    {
        if (painter == null)
            painter = FindObjectOfType<Painter>();
    }

    public void Select()
    {
        if (painter == null)
        {
            Debug.LogError("BrushModeButton: Painter is not assigned.");
            return;
        }

        painter.SetBrushType(brushType);

        if (closePanelAfterSelect != null)
            closePanelAfterSelect.SetActive(false);
    }
}
