using UnityEngine;

public static class BackendUrlProvider
{
    private const string DefaultUrl = "https://spirited-alignment-production-1966.up.railway.app";
    private const string PlayerPrefsKey = "mart.backend.url";

    /// <summary>
    /// LAN IP of the dev PC for phone builds over Wi‑Fi.
    /// Example: http://192.168.1.5:3001 — set once before Build.
    /// Leave empty when using USB + adb reverse.
    /// </summary>
    public const string DeviceLanUrl = "";

    public static string GetBaseUrl(string serializedUrl = null)
    {
        // PlayerPrefs override wins (удобно для отладки против локального бэка).
        string overrideUrl = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(overrideUrl))
        {
            return overrideUrl.Trim().TrimEnd('/');
        }

        string candidate = string.IsNullOrWhiteSpace(serializedUrl) ? DefaultUrl : serializedUrl.Trim();

        // Если в сцене/префабе остался старый localhost (или поле пустое) —
        // используем боевой DefaultUrl, чтобы не зависеть от значения в Инспекторе.
        if (IsLocalhost(candidate))
        {
            return DefaultUrl.TrimEnd('/');
        }

        return candidate.TrimEnd('/');
    }

    public static bool IsLocalhost(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return true;
        }

        return url.Contains("localhost") || url.Contains("127.0.0.1");
    }
}
