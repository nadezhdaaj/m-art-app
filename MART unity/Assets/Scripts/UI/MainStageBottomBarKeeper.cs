using UnityEngine;

/// <summary>
/// Keeps the main bottom navigation bar drawn above full-screen panels (Profile, Home, etc.).
/// </summary>
[DisallowMultipleComponent]
public class MainStageBottomBarKeeper : MonoBehaviour
{
    private void Awake()
    {
        EnsureBottomBarOnTop();
    }

    private void OnEnable()
    {
        EnsureBottomBarOnTop();
    }

    private void OnTransformParentChanged()
    {
        EnsureBottomBarOnTop();
    }

    public void EnsureBottomBarOnTop()
    {
        transform.SetAsLastSibling();
    }
}
