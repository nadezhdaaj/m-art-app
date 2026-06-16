#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Restores ARKit test assets if an editor tool accidentally modified immutable package files.
/// </summary>
[InitializeOnLoad]
public static class ImmutablePackageRestore
{
    private const string AlteredAssetPath =
        "Packages/com.unity.xr.arkit/Tests/Editor/Assets/TestReferenceImageLibrary.asset";

    static ImmutablePackageRestore()
    {
        EditorApplication.delayCall += TryRestoreAlteredImmutableAsset;
    }

    private static void TryRestoreAlteredImmutableAsset()
    {
        if (!File.Exists(AlteredAssetPath))
        {
            return;
        }

        string packageCacheRoot = Path.Combine(Directory.GetCurrentDirectory(), "Library", "PackageCache");
        if (!Directory.Exists(packageCacheRoot))
        {
            return;
        }

        string[] packageDirs = Directory.GetDirectories(packageCacheRoot, "com.unity.xr.arkit@*");
        if (packageDirs.Length == 0)
        {
            return;
        }

        string sourcePath = Path.Combine(
            packageDirs[0],
            "Tests",
            "Editor",
            "Assets",
            "TestReferenceImageLibrary.asset");

        if (!File.Exists(sourcePath))
        {
            return;
        }

        byte[] installed = File.ReadAllBytes(AlteredAssetPath);
        byte[] pristine = File.ReadAllBytes(sourcePath);
        if (installed.SequenceEqual(pristine))
        {
            return;
        }

        File.Copy(sourcePath, AlteredAssetPath, true);
        AssetDatabase.ImportAsset(AlteredAssetPath, ImportAssetOptions.ForceUpdate);
        Debug.Log("Восстановлен файл пакета ARKit (immutable package warning устранён).");
    }
}
#endif
