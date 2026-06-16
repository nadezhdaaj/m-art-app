using TMPro;
using UnityEngine;

/// <summary>
/// Keeps OtherPanel scroll limits in sync when notes content grows or shrinks.
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class OtherPanelNotesScrollAdapter : MonoBehaviour
{
    private void OnEnable()
    {
        BindInputFields();
        OtherPanelScrollbarController.RefreshLayout();
    }

    private void OnTransformChildrenChanged()
    {
        OtherPanelScrollbarController.RefreshLayout();
    }

    private void BindInputFields()
    {
        TMP_InputField[] fields = GetComponentsInChildren<TMP_InputField>(true);
        for (int i = 0; i < fields.Length; i++)
        {
            TMP_InputField field = fields[i];
            if (field == null)
            {
                continue;
            }

            field.onValueChanged.RemoveListener(OnNotesContentChanged);
            field.onValueChanged.AddListener(OnNotesContentChanged);
        }
    }

    private void OnNotesContentChanged(string _)
    {
        OtherPanelScrollbarController.RefreshLayout();
    }
}
