using UnityEngine;

public static class SessionStorage
{
    private const string TokenKey = "mart.auth.token";

    public static void SaveToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        PlayerPrefs.SetString(TokenKey, token);
        PlayerPrefs.Save();
    }

    public static string LoadToken()
    {
        return PlayerPrefs.GetString(TokenKey, string.Empty);
    }

    public static void ClearToken()
    {
        PlayerPrefs.DeleteKey(TokenKey);
        PlayerPrefs.Save();
    }
}
