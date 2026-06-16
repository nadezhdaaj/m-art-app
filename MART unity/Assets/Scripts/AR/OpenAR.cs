using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenARScene : MonoBehaviour
{
    public enum ArEntryMode
    {
        Scanning = 0,
        Photo = 1,
    }

    public const string OpenHomeOnMainStageKey = "OpenHomeOnMainStage";
    public const string PendingExhibitIdKey = "PendingExhibitId";
    public const string EntryModeKey = "ArEntryMode";

    public static ArEntryMode CurrentEntryMode { get; private set; } = ArEntryMode.Scanning;

    public void OpenAR()
    {
        LoadArScene(ArEntryMode.Scanning, clearPendingExhibit: true);
    }

    public void OpenARPhotoMode()
    {
        LoadArScene(ArEntryMode.Photo, clearPendingExhibit: true);
    }

    public void OpenARForExhibit(string exhibitId)
    {
        if (!string.IsNullOrWhiteSpace(exhibitId))
        {
            PlayerPrefs.SetString(PendingExhibitIdKey, exhibitId);
        }
        else
        {
            PlayerPrefs.DeleteKey(PendingExhibitIdKey);
        }

        LoadArScene(ArEntryMode.Scanning, clearPendingExhibit: false);
    }

    public static ArEntryMode ConsumeEntryMode()
    {
        bool hasStoredMode = PlayerPrefs.HasKey(EntryModeKey);
        if (!hasStoredMode)
        {
#if UNITY_EDITOR
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.name == "ARScene")
            {
                CurrentEntryMode = ArEntryMode.Photo;
                return CurrentEntryMode;
            }
#endif
            CurrentEntryMode = ArEntryMode.Scanning;
            return CurrentEntryMode;
        }

        int raw = PlayerPrefs.GetInt(EntryModeKey, (int)CurrentEntryMode);
        PlayerPrefs.DeleteKey(EntryModeKey);
        PlayerPrefs.Save();

        CurrentEntryMode = raw == (int)ArEntryMode.Photo ? ArEntryMode.Photo : ArEntryMode.Scanning;
        return CurrentEntryMode;
    }

    public static void PrepareArSceneLoad(ArEntryMode mode)
    {
        CurrentEntryMode = mode;
        PlayerPrefs.SetInt(EntryModeKey, (int)mode);
        PlayerPrefs.Save();
    }

    public static string ConsumePendingExhibitId()
    {
        string exhibitId = PlayerPrefs.GetString(PendingExhibitIdKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(exhibitId))
        {
            PlayerPrefs.DeleteKey(PendingExhibitIdKey);
            PlayerPrefs.Save();
        }

        return exhibitId;
    }

    public void BackToMainStageHome()
    {
        PlayerPrefs.SetInt(OpenHomeOnMainStageKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("The main stage");
    }

    private static void LoadArScene(ArEntryMode mode, bool clearPendingExhibit)
    {
        CurrentEntryMode = mode;

        if (clearPendingExhibit)
        {
            PlayerPrefs.DeleteKey(PendingExhibitIdKey);
        }

        PlayerPrefs.SetInt(EntryModeKey, (int)mode);
        PlayerPrefs.Save();
        SceneManager.LoadScene("ARScene");
    }
}
