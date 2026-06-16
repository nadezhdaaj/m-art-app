#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GuideChipPrefabCreator
{
    private const string PrefabPath = "Assets/Resources/Guide/GuideChip.prefab";

    [MenuItem("MART/Guide/Apply Recommended Chip Layout")]
    public static void ApplyRecommendedChipLayout()
    {
        Transform chips = FindChipsContainer();
        if (chips == null)
        {
            Debug.LogError("Guide: объект Chips не найден.");
            return;
        }

        EnsureGridOnChips(chips.gameObject, 350f, 72f, 14f, 12f);

        RectTransform chipsRect = chips as RectTransform;
        if (chipsRect != null)
            chipsRect.sizeDelta = new Vector2(1100f, 170f);

        for (int i = 0; i < chips.childCount; i++)
        {
            Transform child = chips.GetChild(i);
            if (child.name.Contains("ChipTemplate"))
                continue;

            TMPro.TMP_Text tmp = child.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmp == null)
                continue;

            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 14f;
            tmp.fontSizeMax = 22f;
            tmp.enableWordWrapping = true;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.margin = new Vector4(8f, 4f, 8f, 4f);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Guide: применён рекомендуемый размер сетки чипов (350x72).");
    }

    [MenuItem("MART/Guide/Create 6 Static Chips In Scene")]
    public static void CreateSixStaticChipsInScene()
    {
        Transform chips = FindChipsContainer();
        GameObject template = FindChipTemplate(includeInactive: true);
        if (chips == null || template == null)
        {
            Debug.LogError("Guide: нужны объекты Chips и ChipTemplate button на сцене The main stage.");
            return;
        }

        if (template.transform.parent == chips)
        {
            template.transform.SetParent(chips.parent, false);
        }

        template.SetActive(false);

        for (int i = chips.childCount - 1; i >= 0; i--)
        {
            Transform child = chips.GetChild(i);
            if (child.name.Contains("ChipTemplate"))
                continue;

            Object.DestroyImmediate(child.gameObject);
        }

        GuideSuggestionItem[] items =
        {
            new GuideSuggestionItem { topicId = "museum", label = "Что особенного в музее?" },
            new GuideSuggestionItem { topicId = "artists", label = "Кто художники в экспозиции?" },
            new GuideSuggestionItem { topicId = "residency", label = "Что такое арт-резиденция?" },
            new GuideSuggestionItem { topicId = "events", label = "Какие мероприятия бывают?" },
            new GuideSuggestionItem { topicId = "building", label = "Расскажи про здание музея" },
            new GuideSuggestionItem { topicId = "photo", label = "Можно ли фотографировать?" }
        };

        foreach (GuideSuggestionItem item in items)
        {
            GameObject chip = Object.Instantiate(template, chips);
            chip.name = "Chip_" + item.topicId;
            chip.SetActive(true);

            TMPro.TMP_Text label = chip.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (label != null)
                label.text = item.label;
        }

        EnsureGridOnChips(chips.gameObject, 350f, 72f, 14f, 12f);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Guide: 6 кнопок созданы в Chips. Меняйте их вид в Inspector — они останутся в сцене.");
    }

    [MenuItem("MART/Guide/Create Chip Prefab From Template")]
    public static void CreateFromTemplate()
    {
        GameObject template = FindChipTemplate(includeInactive: true);
        if (template == null)
        {
            Debug.LogError(
                "Guide: не найден ChipTemplate button. Откройте сцену The main stage и проверьте объект в Chat with a guide."
            );
            return;
        }

        EnsureFolder("Assets/Resources/Guide");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(template, PrefabPath);
        AssetDatabase.SaveAssets();
        Debug.Log("Guide: prefab сохранён: " + PrefabPath, prefab);
    }

    [InitializeOnLoadMethod]
    private static void RegisterEditorHooks()
    {
        EditorApplication.delayCall += TryEnsureStaticChipsInScene;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        AutoCreatePrefabOnce();
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += TryEnsureStaticChipsInScene;
    }

    /// <summary>
    /// Если под Chips пусто — создаёт 6 кнопок в сцене (видны без Play).
    /// </summary>
    private static void TryEnsureStaticChipsInScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Transform chips = FindChipsContainer();
        if (chips == null)
            return;

        if (CountChipChildren(chips) > 0)
            return;

        GameObject template = FindChipTemplate(includeInactive: true);
        if (template == null)
            return;

        CreateSixStaticChipsInScene();
    }

    private static int CountChipChildren(Transform chips)
    {
        int count = 0;
        for (int i = 0; i < chips.childCount; i++)
        {
            if (chips.GetChild(i).name.Contains("ChipTemplate"))
                continue;

            count++;
        }

        return count;
    }

    private static void AutoCreatePrefabOnce()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            return;

        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;

            GameObject template = FindChipTemplate(includeInactive: true);
            if (template == null)
                return;

            EnsureFolder("Assets/Resources/Guide");
            PrefabUtility.SaveAsPrefabAsset(template, PrefabPath);
            AssetDatabase.SaveAssets();
        };
    }

    private static GameObject FindChipTemplate(bool includeInactive)
    {
        if (includeInactive)
        {
            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject go in all)
            {
                if (go == null || go.name != "ChipTemplate button")
                    continue;

                if (EditorUtility.IsPersistent(go))
                    continue;

                if (!go.scene.IsValid())
                    continue;

                return go;
            }

            return null;
        }

        return GameObject.Find("ChipTemplate button");
    }

    private static Transform FindChipsContainer()
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in all)
        {
            if (go == null || go.name != "Chips")
                continue;

            if (EditorUtility.IsPersistent(go))
                continue;

            if (!go.scene.IsValid())
                continue;

            return go.transform;
        }

        return null;
    }

    private static void EnsureGridOnChips(
        GameObject chipsGo,
        float cellWidth = 350f,
        float cellHeight = 72f,
        float spacingX = 14f,
        float spacingY = 12f)
    {
        VerticalLayoutGroup vertical = chipsGo.GetComponent<VerticalLayoutGroup>();
        if (vertical != null)
            vertical.enabled = false;

        GridLayoutGroup grid = chipsGo.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = chipsGo.AddComponent<GridLayoutGroup>();

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.cellSize = new Vector2(cellWidth, cellHeight);
        grid.spacing = new Vector2(spacingX, spacingY);
        grid.childAlignment = TextAnchor.UpperCenter;
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Guide"))
            AssetDatabase.CreateFolder("Assets/Resources", "Guide");
    }
}
#endif
