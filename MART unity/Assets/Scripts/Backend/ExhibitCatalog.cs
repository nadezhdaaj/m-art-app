using System;
using System.Collections.Generic;
using UnityEngine;

public static class ExhibitCatalog
{
    private static readonly string[] DefaultExhibitIds =
    {
        "painting_1",
        "painting_2",
        "painting_3",
    };
    private static readonly Color ActiveHeartColor = Color.white;
    private static readonly Color FavoriteHeartColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    public static Color GetActiveHeartColor()
    {
        return ActiveHeartColor;
    }

    public static Color GetFavoriteHeartColor()
    {
        return FavoriteHeartColor;
    }

    public static int GetCoverIndexFromExhibitId(string exhibitId)
    {
        if (string.IsNullOrWhiteSpace(exhibitId) || !exhibitId.StartsWith("painting_", StringComparison.Ordinal))
        {
            return -1;
        }

        string suffix = exhibitId.Substring("painting_".Length);
        return int.TryParse(suffix, out int coverIndex) ? coverIndex : -1;
    }

    public static Sprite GetPreviewSprite(string exhibitId)
    {
        if (string.IsNullOrWhiteSpace(exhibitId))
        {
            return null;
        }

        return Resources.Load<Sprite>("Exhibits/" + exhibitId);
    }

    public static IReadOnlyList<string> GetExhibitIdsForPhotoMode()
    {
        Sprite[] previews = Resources.LoadAll<Sprite>("Exhibits");
        if (previews != null && previews.Length > 0)
        {
            var ids = new List<string>(previews.Length);
            for (int i = 0; i < previews.Length; i++)
            {
                if (previews[i] != null && !string.IsNullOrWhiteSpace(previews[i].name))
                {
                    ids.Add(previews[i].name);
                }
            }

            ids.Sort(StringComparer.Ordinal);
            return ids;
        }

        return DefaultExhibitIds;
    }

    public static int GetExhibitIndex(string exhibitId, IReadOnlyList<string> exhibitIds)
    {
        if (exhibitIds == null || string.IsNullOrWhiteSpace(exhibitId))
        {
            return 0;
        }

        for (int i = 0; i < exhibitIds.Count; i++)
        {
            if (exhibitIds[i] == exhibitId)
            {
                return i;
            }
        }

        return 0;
    }

    public static Color GetPhotoModeFallbackColor(int exhibitIndex)
    {
        Color[] colors =
        {
            new Color(0.85f, 0.35f, 0.25f),
            new Color(0.25f, 0.55f, 0.9f),
            new Color(0.35f, 0.75f, 0.4f),
            new Color(0.9f, 0.75f, 0.2f),
        };

        return colors[exhibitIndex % colors.Length];
    }
}
