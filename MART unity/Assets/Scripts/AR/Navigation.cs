using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class Navigation : MonoBehaviour
{
    public GameObject homeScreen;
    public GameObject gamesScreen;
    public GameObject libraryScreen;
    public GameObject profileScreen;

    public Image homeIcon;
    public Image gamesIcon;
    public Image libraryIcon;
    public Image profileIcon;

    public RectTransform indicator;

    public float moveSpeed = 8f;
    public float iconScale = 1.2f;

    private static readonly Color ActiveIconColor = Color.gray;
    private static readonly Color InactiveIconColor = Color.white;

    void Awake()
    {
        RepairReferences();
        NormalizeBottomBarButtons();
        StartDeferredSync();
    }

    void OnEnable()
    {
        RepairReferences();
        NormalizeBottomBarButtons();
        SyncToActiveScreen();
        StartDeferredSync();
    }

    void Start()
    {
        if (PlayerPrefs.GetInt(OpenARScene.OpenHomeOnMainStageKey, 0) == 1)
        {
            PlayerPrefs.DeleteKey(OpenARScene.OpenHomeOnMainStageKey);
            OpenHome();
            return;
        }

        SyncToActiveScreen();
        StartDeferredSync();
        MainStageArButtonsSetup.EnsureWired();
    }

    public void OpenHome()
    {
        ShowScreen(homeScreen, homeIcon);
        MainStageArButtonsSetup.EnsureWired();
    }

    public void OpenGames()
    {
        ShowScreen(gamesScreen, gamesIcon);
    }

    public void OpenLibrary()
    {
        ShowScreen(libraryScreen, libraryIcon);
    }

    public void OpenProfile()
    {
        ShowScreen(profileScreen, profileIcon);
        ProfilePanelDefaultVisibility.Apply();
    }

    void ShowScreen(GameObject screen, Image activeIcon)
    {
        SetScreen(screen);
        SetIcons(activeIcon);
        MoveToIcon(activeIcon);
        ClearSelectedUiObject();
    }

    void SetScreen(GameObject screen)
    {
        if (homeScreen != null)
        {
            homeScreen.SetActive(false);
        }

        if (gamesScreen != null)
        {
            gamesScreen.SetActive(false);
        }

        if (libraryScreen != null)
        {
            libraryScreen.SetActive(false);
        }

        if (profileScreen != null)
        {
            profileScreen.SetActive(false);
        }

        if (screen != null)
        {
            screen.SetActive(true);
        }
    }

    void SetIcons(Image activeIcon)
    {
        Image[] icons = { homeIcon, gamesIcon, libraryIcon, profileIcon };

        foreach (Image icon in icons)
        {
            if (icon == null)
            {
                continue;
            }

            icon.color = InactiveIconColor;
            icon.transform.localScale = Vector3.one;
        }

        if (activeIcon == null)
        {
            return;
        }

        activeIcon.color = ActiveIconColor;
        activeIcon.transform.localScale = Vector3.one * iconScale;
    }

    void MoveToIcon(Image icon)
    {
        if (indicator == null || icon == null)
        {
            return;
        }

        StopAllCoroutines();
        StartCoroutine(MoveIndicator(indicator.anchoredPosition, icon.rectTransform.anchoredPosition));
    }

    IEnumerator MoveIndicator(Vector2 startPos, Vector2 targetPos)
    {
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * moveSpeed;
            indicator.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        indicator.anchoredPosition = targetPos;
    }

    void SyncToActiveScreen()
    {
        if (homeScreen != null && homeScreen.activeInHierarchy)
        {
            SetIcons(homeIcon);
            MoveToIcon(homeIcon);
            return;
        }

        if (gamesScreen != null && gamesScreen.activeInHierarchy)
        {
            SetIcons(gamesIcon);
            MoveToIcon(gamesIcon);
            return;
        }

        if (libraryScreen != null && libraryScreen.activeInHierarchy)
        {
            SetIcons(libraryIcon);
            MoveToIcon(libraryIcon);
            return;
        }

        if (profileScreen != null && profileScreen.activeInHierarchy)
        {
            SetIcons(profileIcon);
            MoveToIcon(profileIcon);
            return;
        }

        OpenHome();
    }

    void NormalizeBottomBarButtons()
    {
        DisableButtonTint(homeIcon);
        DisableButtonTint(gamesIcon);
        DisableButtonTint(libraryIcon);
        DisableButtonTint(profileIcon);
    }

    void DisableButtonTint(Image icon)
    {
        if (icon == null || !icon.TryGetComponent<Button>(out Button button))
        {
            return;
        }

        button.transition = Selectable.Transition.None;
    }

    void RepairReferences()
    {
        homeScreen = ResolveScreen(homeScreen, "Home");
        gamesScreen = ResolveScreen(gamesScreen, "Mini games", "Games");
        libraryScreen = ResolveScreen(libraryScreen, "Library");
        profileScreen = ResolveScreen(profileScreen, "ProfileUI", "Profile");

        homeIcon = ResolveIcon(homeIcon, "Home");
        gamesIcon = ResolveIcon(gamesIcon, "Games");
        libraryIcon = ResolveIcon(libraryIcon, "Library");
        profileIcon = ResolveIcon(profileIcon, "Profile");

        if (indicator == null)
        {
            Transform indicatorTransform = transform.Find("Indicator");
            if (indicatorTransform is RectTransform rectTransform)
            {
                indicator = rectTransform;
            }
        }
    }

    GameObject ResolveScreen(GameObject currentScreen, params string[] candidateNames)
    {
        if (currentScreen != null && currentScreen.transform.IsChildOf(transform))
        {
            currentScreen = null;
        }
        else if (currentScreen != null && currentScreen.transform.parent != transform.parent)
        {
            return currentScreen;
        }

        Transform parentTransform = transform.parent;
        if (parentTransform == null)
        {
            return currentScreen;
        }

        foreach (string candidateName in candidateNames)
        {
            Transform candidate = parentTransform.Find(candidateName);
            if (candidate != null && candidate != transform)
            {
                return candidate.gameObject;
            }
        }

        return currentScreen;
    }

    Image ResolveIcon(Image currentIcon, string candidateName)
    {
        if (currentIcon != null && currentIcon.transform.parent == transform)
        {
            return currentIcon;
        }

        Transform iconTransform = transform.Find(candidateName);
        if (iconTransform == null)
        {
            return currentIcon;
        }

        return iconTransform.GetComponent<Image>();
    }

    void ClearSelectedUiObject()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    void StartDeferredSync()
    {
        StopCoroutine(nameof(DeferredSyncToActiveScreen));
        StartCoroutine(nameof(DeferredSyncToActiveScreen));
    }

    IEnumerator DeferredSyncToActiveScreen()
    {
        yield return null;
        RepairReferences();
        SyncToActiveScreen();
    }
}
