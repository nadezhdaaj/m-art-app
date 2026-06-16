using System;
using System.Collections.Generic;
using UnityEngine;

public static class ExhibitFavoritesStore
{
    public static event Action FavoritesChanged;

    private const string LocalStorageKey = "exhibit.favorites.local.v1";
    private const char Separator = ';';

    private static readonly HashSet<string> FavoriteIds = new HashSet<string>(StringComparer.Ordinal);
    private static bool isLoaded;

    public static bool IsFavorite(string exhibitId)
    {
        EnsureLoaded();
        return !string.IsNullOrWhiteSpace(exhibitId) && FavoriteIds.Contains(exhibitId);
    }

    public static IReadOnlyCollection<string> GetAll()
    {
        EnsureLoaded();
        return FavoriteIds;
    }

    public static void SetFavorites(IEnumerable<string> exhibitIds)
    {
        EnsureLoaded();
        FavoriteIds.Clear();

        if (exhibitIds != null)
        {
            foreach (string exhibitId in exhibitIds)
            {
                if (!string.IsNullOrWhiteSpace(exhibitId))
                {
                    FavoriteIds.Add(exhibitId);
                }
            }
        }

        PersistAndNotify();
    }

    public static void AddLocal(string exhibitId)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(exhibitId))
        {
            return;
        }

        if (FavoriteIds.Add(exhibitId))
        {
            PersistAndNotify();
        }
    }

    public static void RemoveLocal(string exhibitId)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(exhibitId))
        {
            return;
        }

        if (FavoriteIds.Remove(exhibitId))
        {
            PersistAndNotify();
        }
    }

    public static void Clear()
    {
        EnsureLoaded();
        if (FavoriteIds.Count == 0)
        {
            return;
        }

        FavoriteIds.Clear();
        PersistAndNotify();
    }

    public static void MergeFromRemote(IEnumerable<string> remoteFavorites)
    {
        EnsureLoaded();
        if (remoteFavorites == null)
        {
            return;
        }

        bool changed = false;
        foreach (string exhibitId in remoteFavorites)
        {
            if (!string.IsNullOrWhiteSpace(exhibitId) && FavoriteIds.Add(exhibitId))
            {
                changed = true;
            }
        }

        if (changed)
        {
            PersistAndNotify();
        }
    }

    private static void EnsureLoaded()
    {
        if (isLoaded)
        {
            return;
        }

        isLoaded = true;
        string raw = PlayerPrefs.GetString(LocalStorageKey, string.Empty);
        if (string.IsNullOrEmpty(raw))
        {
            return;
        }

        string[] parts = raw.Split(Separator);
        for (int i = 0; i < parts.Length; i++)
        {
            string id = parts[i];
            if (!string.IsNullOrWhiteSpace(id))
            {
                FavoriteIds.Add(id);
            }
        }
    }

    private static void PersistAndNotify()
    {
        Persist();
        FavoritesChanged?.Invoke();
    }

    private static void Persist()
    {
        if (FavoriteIds.Count == 0)
        {
            PlayerPrefs.DeleteKey(LocalStorageKey);
        }
        else
        {
            string joined = string.Join(Separator.ToString(), FavoriteIds);
            PlayerPrefs.SetString(LocalStorageKey, joined);
        }

        PlayerPrefs.Save();
    }
}
