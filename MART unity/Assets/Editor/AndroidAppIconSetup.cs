#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Applies Android app icons to every required size slot (fixes missing res.ldpi.app_icon.png on build).
/// Uses PlayerSettings + SerializedObject (compatible with Unity 2022.3).
/// </summary>
public static class AndroidAppIconSetup
{
    private const string LogoPath = "Assets/UI/The logo (4).png";
    private const string IconPath = "Assets/AppIcons/AndroidAppIcon.png";

    [InitializeOnLoadMethod]
    private static void ApplyUncompressedIconOnLoad()
    {
        EditorApplication.delayCall += () => ApplyAppIconFromLogo(silent: true);
    }

    [MenuItem("Tools/Android/Apply App Icon From Logo")]
    public static void ApplyAppIconFromLogoMenu()
    {
        ApplyAppIconFromLogo(silent: false);
    }

    public static void ApplyAppIconFromLogo(bool silent = false)
    {
        if (!File.Exists(LogoPath))
        {
            Debug.LogError($"Icon source not found: {LogoPath}");
            return;
        }

        EnsureAppIconCopyExists();
        ConfigureIconImporter(IconPath);
        AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceSynchronousImport);

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon == null)
        {
            Debug.LogError($"Failed to load icon texture at {IconPath}");
            return;
        }

        ApplyLegacyAndroidIcons(icon);
        ApplyPlatformAndroidIcons(icon);
        AssetDatabase.SaveAssets();

        if (!silent)
        {
            Debug.Log("Android icons applied. Rebuild APK.");
        }
    }

    private static void EnsureAppIconCopyExists()
    {
        Directory.CreateDirectory("Assets/AppIcons");
        File.Copy(LogoPath, IconPath, true);
    }

    private static void ConfigureIconImporter(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 512;
        importer.isReadable = false;

        TextureImporterPlatformSettings defaultPlatform = importer.GetDefaultPlatformTextureSettings();
        defaultPlatform.textureCompression = TextureImporterCompression.Uncompressed;
        defaultPlatform.overridden = true;
        defaultPlatform.maxTextureSize = 512;
        importer.SetPlatformTextureSettings(defaultPlatform);

        TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
        android.textureCompression = TextureImporterCompression.Uncompressed;
        android.overridden = true;
        android.maxTextureSize = 512;
        importer.SetPlatformTextureSettings(android);

        importer.SaveAndReimport();
    }

    private static void ApplyLegacyAndroidIcons(Texture2D icon)
    {
        int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
        if (sizes == null || sizes.Length == 0)
        {
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { icon });
            return;
        }

        var icons = new Texture2D[sizes.Length];
        for (int i = 0; i < icons.Length; i++)
        {
            icons[i] = icon;
        }

        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, icons);
    }

    private static void ApplyPlatformAndroidIcons(Texture2D icon)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (assets == null || assets.Length == 0)
        {
            return;
        }

        var settings = new SerializedObject(assets[0]);
        SerializedProperty platformIcons = settings.FindProperty("m_BuildTargetPlatformIcons");
        if (platformIcons == null || !platformIcons.isArray)
        {
            settings.ApplyModifiedPropertiesWithoutUndo();
            return;
        }

        for (int i = 0; i < platformIcons.arraySize; i++)
        {
            SerializedProperty entry = platformIcons.GetArrayElementAtIndex(i);
            SerializedProperty buildTarget = entry.FindPropertyRelative("m_BuildTarget");
            if (buildTarget == null)
            {
                continue;
            }

            bool isAndroid = buildTarget.type == "string"
                ? buildTarget.stringValue == "Android"
                : buildTarget.intValue == (int)BuildTarget.Android;

            if (!isAndroid)
            {
                continue;
            }

            SerializedProperty icons = entry.FindPropertyRelative("m_Icons");
            if (icons == null || !icons.isArray)
            {
                continue;
            }

            for (int j = 0; j < icons.arraySize; j++)
            {
                SerializedProperty textures = icons.GetArrayElementAtIndex(j).FindPropertyRelative("m_Textures");
                if (textures == null || !textures.isArray)
                {
                    continue;
                }

                textures.arraySize = 1;
                textures.GetArrayElementAtIndex(0).objectReferenceValue = icon;
            }
        }

        settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
