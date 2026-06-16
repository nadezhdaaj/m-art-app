using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Persistent local storage for AR photos shown in the profile gallery.
/// Files are stored under Application.persistentDataPath / "ARPhotos" / userId.
/// </summary>
public static class ARPhotoLibrary
{
    private const string RootFolderName = "ARPhotos";
    private const string GuestUserId = "guest";

    public static event Action LibraryChanged;

    public static string SavePhoto(Texture2D photo)
    {
        if (photo == null)
        {
            return null;
        }

        string folder = GetUserPhotosFolder();
        EnsureDirectoryExists(folder);

        string fileName = "ARPhoto_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".png";
        string fullPath = Path.Combine(folder, fileName);

        try
        {
            byte[] bytes = photo.EncodeToPNG();
            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogWarning("AR Photo: EncodeToPNG вернул пустые данные (текстура нечитаемая?).");
                return null;
            }

            File.WriteAllBytes(fullPath, bytes);
            Debug.Log("AR Photo: фото сохранено в " + fullPath);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return null;
        }

        LibraryChanged?.Invoke();
        return fullPath;
    }

    public static List<string> GetSavedPhotoPaths()
    {
        var result = new List<string>();
        string folder = GetUserPhotosFolder();
        if (!Directory.Exists(folder))
        {
            return result;
        }

        try
        {
            string[] files = Directory.GetFiles(folder, "*.png");
            Array.Sort(files);
            Array.Reverse(files);
            result.AddRange(files);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }

        return result;
    }

    public static Texture2D LoadPhoto(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(bytes))
            {
                return texture;
            }

            UnityEngine.Object.Destroy(texture);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }

        return null;
    }

    public static void DeletePhoto(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                LibraryChanged?.Invoke();
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static string GetUserPhotosFolder()
    {
        string userId = ResolveUserId();
        return Path.Combine(Application.persistentDataPath, RootFolderName, userId);
    }

    private static string ResolveUserId()
    {
        if (UserSession.Instance != null && !string.IsNullOrWhiteSpace(UserSession.Instance.UserId))
        {
            return SanitizeFileName(UserSession.Instance.UserId);
        }

        return GuestUserId;
    }

    private static string SanitizeFileName(string raw)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        var builder = new System.Text.StringBuilder(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        }

        return builder.ToString();
    }

    private static void EnsureDirectoryExists(string folder)
    {
        try
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
