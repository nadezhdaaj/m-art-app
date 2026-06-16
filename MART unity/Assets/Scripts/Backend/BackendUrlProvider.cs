using UnityEngine;

public static class BackendUrlProvider
{
    private const string DefaultUrl = "http://localhost:3001";
    private const string PlayerPrefsKey = "mart.backend.url";

    /// <summary>
    /// LAN IP of the dev PC for phone builds over Wi‑Fi.
    /// Example: http://192.168.1.5:3001 — set once before Build.
    /// Leave empty when using USB + adb reverse.
    /// </summary>
    public const string DeviceLanUrl = "http://192.168.0.11:3001";

    public static string GetBaseUrl(string serializedUrl = null)
    {
        string candidate = string.IsNullOrWhiteSpace(serializedUrl) ? DefaultUrl : serializedUrl.Trim();

        string overrideUrl = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(overrideUrl))
        {
            return overrideUrl.Trim().TrimEnd('/');
        }

#if UNITY_EDITOR
        return candidate.TrimEnd('/');
#else
        if (IsLocalhost(candidate) && !string.IsNullOrWhiteSpace(DeviceLanUrl))
        {
            return DeviceLanUrl.Trim().TrimEnd('/');
        }

        return candidate.TrimEnd('/');
#endif
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
