using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class LocalProfileProgressWidget : MonoBehaviour
{
    private const string TitleValueName = "Title";

    private TMP_Text titleValueText;
    private bool initialized;

    public void InitializeWithTitle(TMP_Text titleText)
    {
        if (titleText == null)
        {
            return;
        }

        titleValueText = titleText;
        initialized = true;
    }

    private void OnEnable()
    {
        EnsureInitialized();
        Refresh();
        LocalProfileProgression.Changed += Refresh;
    }

    private void OnDisable()
    {
        LocalProfileProgression.Changed -= Refresh;
    }

    public void Refresh()
    {
        EnsureInitialized();
        if (!initialized)
        {
            return;
        }

        LocalProfileProgression.ProgressState state = LocalProfileProgression.GetProgressState();
        titleValueText.text = state.CurrentTitle;
        titleValueText.gameObject.SetActive(true);
    }

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        titleValueText = FindChildText(transform, TitleValueName);
        initialized = titleValueText != null;
    }

    private static TMP_Text FindChildText(Transform root, string objectName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == objectName)
            {
                return children[i].GetComponent<TMP_Text>();
            }
        }

        return null;
    }
}
