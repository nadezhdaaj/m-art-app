using UnityEditor;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Одноразовый редакторный помощник: добавляет картинку DSC_8069 в библиотеку
/// маркеров MuseumImages.asset через официальный AR Foundation API
/// (GUID кодируется самим API — руками ничего считать не нужно).
///
/// Запуск: меню  Tools > AR > Add DSC_8069 to MuseumImages.
/// После успешного добавления этот файл можно удалить.
/// </summary>
public static class AddDSC8069ToMuseumImages
{
    private const string LibraryPath = "Assets/MuseumImages.asset";
    private const string TexturePath = "Assets/UI/DSC_8069.JPG";
    private const string ImageName = "DSC_8069";

    // Физическая ширина картины в метрах. При желании поменяй под реальный размер
    // печати — высота посчитается автоматически по пропорциям изображения.
    private const float PhysicalWidthMeters = 1.0f;

    [MenuItem("Tools/AR/Add DSC_8069 to MuseumImages")]
    public static void Run()
    {
        var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
        if (library == null)
        {
            Debug.LogError($"[AR] Не найдена библиотека по пути {LibraryPath}");
            return;
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if (texture == null)
        {
            Debug.LogError($"[AR] Не найдена текстура по пути {TexturePath}");
            return;
        }

        // ARCore при сборке читает пиксели эталонной картинки — включаем Read/Write.
        EnsureReadable(TexturePath);

        // Идемпотентность: если запись DSC_8069 уже есть — удаляем, чтобы не плодить дубли.
        for (int i = library.count - 1; i >= 0; i--)
        {
            if (library[i].name == ImageName)
            {
                library.RemoveAt(i);
            }
        }

        // Добавляем новую запись и заполняем её.
        library.Add();
        int index = library.count - 1;

        library.SetName(index, ImageName);
        library.SetTexture(index, texture, false);

        float height = PhysicalWidthMeters;
        if (texture.width > 0)
        {
            height = PhysicalWidthMeters * texture.height / texture.width;
        }
        library.SetSpecifySize(index, true);
        library.SetSize(index, new Vector2(PhysicalWidthMeters, height));

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AR] Готово: '{ImageName}' добавлена в MuseumImages " +
                  $"(размер {PhysicalWidthMeters:0.###} x {height:0.###} м). " +
                  "Теперь привяжи модель в ARImageSpawner (Image Prefabs).");
    }

    private static void EnsureReadable(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        if (!importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }
}
