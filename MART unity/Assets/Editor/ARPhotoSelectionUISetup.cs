#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ARPhotoSelectionUISetup
{
    private const string ArPhotosObjectName = "AR photos";
    private const string IconObjectName = "icon";
    private const string IconPreviewChildName = "IconModelPreview";
    private const string ArScenePath = "Assets/Scenes/ARScene.unity";

    [MenuItem("Tools/AR/Add Photo Selection UI to AR Scene")]
    public static void AddToArScene()
    {
        var scene = EditorSceneManager.OpenScene(ArScenePath, OpenSceneMode.Single);
        GameObject arPhotos = GameObject.Find(ArPhotosObjectName);
        if (arPhotos == null)
        {
            Debug.LogError("Object 'AR photos' not found in ARScene.");
            return;
        }

        if (arPhotos.GetComponent<ARPhotoSelectionUI>() == null)
        {
            arPhotos.AddComponent<ARPhotoSelectionUI>();
        }

        if (arPhotos.GetComponent<ARPhotoExhibitPlacer>() == null)
        {
            arPhotos.AddComponent<ARPhotoExhibitPlacer>();
        }

        if (arPhotos.GetComponent<ARPhotoCapture>() == null)
        {
            arPhotos.AddComponent<ARPhotoCapture>();
        }

        if (arPhotos.GetComponent<ARPhotoPreview>() == null)
        {
            arPhotos.AddComponent<ARPhotoPreview>();
        }

        EnsureIconPreviewComponents(arPhotos.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = arPhotos;
        Debug.Log("AR Photo Selection UI added. Check 'AR photos' and child 'icon' in the Inspector.");
    }

    private static void EnsureIconPreviewComponents(Transform arPhotosRoot)
    {
        Transform icon = arPhotosRoot.Find(IconObjectName);
        if (icon == null)
        {
            return;
        }

        UnityEngine.UI.Image frame = icon.GetComponent<UnityEngine.UI.Image>();
        if (frame != null)
        {
            frame.sprite = null;
            frame.enabled = false;
            frame.raycastTarget = false;
        }

        UnityEngine.UI.Mask mask = icon.GetComponent<UnityEngine.UI.Mask>();
        if (mask != null)
        {
            Object.DestroyImmediate(mask);
        }

        if (icon.GetComponent<ARPhotoIconPreview>() == null)
        {
            icon.gameObject.AddComponent<ARPhotoIconPreview>();
        }

        Transform preview = icon.Find(IconPreviewChildName);
        if (preview == null)
        {
            var previewObject = new GameObject(
                IconPreviewChildName,
                typeof(RectTransform),
                typeof(UnityEngine.UI.RawImage));
            previewObject.transform.SetParent(icon, false);
            preview = previewObject.transform;
        }

        UnityEngine.UI.Image legacyImage = preview.GetComponent<UnityEngine.UI.Image>();
        if (legacyImage != null)
        {
            Object.DestroyImmediate(legacyImage);
        }

        var rect = preview as RectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var rawImage = preview.GetComponent<UnityEngine.UI.RawImage>();
        if (rawImage == null)
        {
            rawImage = preview.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        }

        rawImage.raycastTarget = true;
        rawImage.maskable = false;

        UnityEngine.UI.Button iconButton = icon.GetComponent<UnityEngine.UI.Button>();
        if (iconButton != null)
        {
            iconButton.targetGraphic = rawImage;
        }
    }
}
#endif
