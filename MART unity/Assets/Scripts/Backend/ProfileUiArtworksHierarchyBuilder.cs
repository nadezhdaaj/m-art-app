using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds profile artworks UI hierarchy (See All, User's work screen) for runtime and editor.
/// </summary>
public static class ProfileUiArtworksHierarchyBuilder
{
    public const string UserWorksScreenName = "User's work";
    public const string SeeAllButtonName = "See All";
    public const string OtherPanelName = "OtherPanel";
    public const string GalleryRootName = "UserWorksGalleryRoot";
    public const string BackButtonName = "BackButton";

    public static void EnsureAll(bool registerUndo = false)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        GameObject userWorks = EnsureUserWorksScreen(registerUndo);
        EnsureSeeAllButton(registerUndo);
        ConfigureUserWorksComponents(userWorks, registerUndo);
    }

    public static GameObject EnsureUserWorksScreen(bool registerUndo = false)
    {
        GameObject existing = FindInactive(UserWorksScreenName);
        if (existing != null)
        {
            ApplyUserWorksScreenLayout(existing);
            return existing;
        }

        Transform parent = ResolveUserWorksParent();
        if (parent == null)
        {
            return null;
        }

        GameObject screenObject = new GameObject(
            UserWorksScreenName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        RegisterCreated(screenObject, registerUndo);
        screenObject.transform.SetParent(parent, false);
        screenObject.transform.SetAsLastSibling();

        ApplyUserWorksScreenLayout(screenObject);

        Image background = screenObject.GetComponent<Image>();
        background.color = Color.white;
        background.raycastTarget = true;

        screenObject.SetActive(false);
        return screenObject;
    }

    public static void EnsureSeeAllButton(bool registerUndo = false)
    {
        GameObject otherPanel = GameObject.Find(OtherPanelName);
        if (otherPanel == null)
        {
            return;
        }

        Transform seeAllTransform = FindPreferredSeeAllButton(otherPanel.transform);
        if (seeAllTransform == null)
        {
            Transform parent = ResolveSeeAllParent(otherPanel.transform);
            seeAllTransform = CreateSeeAllButton(parent, registerUndo);
        }

        if (seeAllTransform == null)
        {
            return;
        }

        Transform targetParent = ResolveSeeAllParent(otherPanel.transform);
        if (seeAllTransform.parent != targetParent)
        {
            SetParent(seeAllTransform, targetParent, registerUndo, "Place See All in OtherPanel content");
        }

        seeAllTransform.gameObject.SetActive(true);
        seeAllTransform.SetAsLastSibling();
        ApplySeeAllLayout(seeAllTransform as RectTransform);

        Button seeAllButton = seeAllTransform.GetComponent<Button>();
        if (seeAllButton == null)
        {
            seeAllButton = seeAllTransform.gameObject.AddComponent<Button>();
        }

        seeAllButton.onClick.RemoveListener(UserWorksScreen.ShowScreen);
        seeAllButton.onClick.AddListener(UserWorksScreen.ShowScreen);
    }

    public static void ConfigureUserWorksComponents(GameObject userWorks, bool registerUndo = false)
    {
        if (userWorks == null)
        {
            return;
        }

        if (userWorks.GetComponent<UserWorksScreen>() == null)
        {
            UserWorksScreen screen = userWorks.AddComponent<UserWorksScreen>();
            RegisterCreated(screen, registerUndo);
        }

        EnsureBackButton(userWorks.transform, registerUndo);
        UserWorksGalleryBuilder.Ensure(userWorks);
    }

    private static Transform CreateSeeAllButton(Transform parent, bool registerUndo)
    {
        GameObject buttonObject = new GameObject(
            SeeAllButtonName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));

        RegisterCreated(buttonObject, registerUndo);
        buttonObject.transform.SetParent(parent, false);
        buttonObject.transform.SetAsLastSibling();

        ApplySeeAllLayout(buttonObject.GetComponent<RectTransform>());

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.01f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.None;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        RegisterCreated(labelObject, registerUndo);
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
        }

        label.text = "See All..";
        label.fontSize = 28f;
        label.alignment = TextAlignmentOptions.Right;
        label.color = new Color(0.35f, 0.35f, 0.35f, 1f);
        label.raycastTarget = false;

        return buttonObject.transform;
    }

    private static void EnsureBackButton(Transform userWorksRoot, bool registerUndo)
    {
        Transform backTransform = userWorksRoot.Find(BackButtonName);
        if (backTransform != null)
        {
            backTransform.SetAsLastSibling();
            return;
        }

        GameObject buttonObject = new GameObject(
            BackButtonName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));

        RegisterCreated(buttonObject, registerUndo);
        buttonObject.transform.SetParent(userWorksRoot, false);
        buttonObject.transform.SetAsLastSibling();

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(120f, 56f);
        rect.anchoredPosition = new Vector2(24f, -24f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        RegisterCreated(labelObject, registerUndo);
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = "Назад";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        label.raycastTarget = false;
    }

    private static Transform ResolveUserWorksParent()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        return canvas != null ? canvas.transform : null;
    }

    private static void ApplyUserWorksScreenLayout(GameObject screenObject)
    {
        if (screenObject == null)
        {
            return;
        }

        Transform canvasTransform = ResolveUserWorksParent();
        if (canvasTransform != null && screenObject.transform.parent != canvasTransform)
        {
            screenObject.transform.SetParent(canvasTransform, false);
        }

        screenObject.transform.SetAsLastSibling();
        StretchFullScreen(screenObject.GetComponent<RectTransform>());
    }

    public static void ApplySeeAllLayout(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(180f, 44f);
        rect.anchoredPosition = new Vector2(-24f, -12f);
    }

    private static GameObject FindInactive(string objectName)
    {
        if (objectName == UserWorksScreenName)
        {
            UserWorksScreen screen = Object.FindObjectOfType<UserWorksScreen>(true);
            if (screen != null)
            {
                return screen.gameObject;
            }
        }

        GameObject additionalScreens = GameObject.Find("Additional screens");
        if (additionalScreens != null)
        {
            Transform directChild = additionalScreens.transform.Find(objectName);
            if (directChild != null && objectName == UserWorksScreenName)
            {
                return directChild.gameObject;
            }
        }

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas != null && objectName == UserWorksScreenName)
        {
            Transform canvasChild = canvas.transform.Find(objectName);
            if (canvasChild != null)
            {
                return canvasChild.gameObject;
            }
        }

        if (objectName == SeeAllButtonName)
        {
            GameObject otherPanel = GameObject.Find(OtherPanelName);
            if (otherPanel != null)
            {
                Transform seeAll = otherPanel.transform.Find(SeeAllButtonName);
                if (seeAll != null)
                {
                    return seeAll.gameObject;
                }
            }
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void StretchFullScreen(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Transform ResolveSeeAllParent(Transform otherPanel)
    {
        Transform scrollContent = otherPanel.Find(OtherPanelScrollbarController.ScrollContentName);
        return scrollContent != null ? scrollContent : otherPanel;
    }

    private static Transform FindPreferredSeeAllButton(Transform otherPanel)
    {
        Transform[] children = otherPanel.GetComponentsInChildren<Transform>(true);
        Transform fallback = null;
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || child.name != SeeAllButtonName)
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = child;
            }

            if (child.gameObject.activeInHierarchy && child.gameObject.layer == 5)
            {
                return child;
            }
        }

        return fallback;
    }

    private static void SetParent(Transform child, Transform parent, bool registerUndo, string undoName)
    {
#if UNITY_EDITOR
        if (registerUndo && !Application.isPlaying)
        {
            UnityEditor.Undo.SetTransformParent(child, parent, undoName);
            return;
        }
#endif
        child.SetParent(parent, false);
    }

    private static void RegisterCreated(Object created, bool registerUndo)
    {
#if UNITY_EDITOR
        if (registerUndo && created != null)
        {
            UnityEditor.Undo.RegisterCreatedObjectUndo(created, "Setup Profile Artworks UI");
        }
#endif
    }
}
