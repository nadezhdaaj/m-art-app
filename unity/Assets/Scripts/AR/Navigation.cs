using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Navigation : MonoBehaviour
{
    public GameObject homeScreen;
    public GameObject gamesScreen;
    public GameObject libraryScreen;
    public GameObject profileScreen;

    public Image homeIcon;
    public Image gamesIcon;
    public Image libraryIcon;
    public Image profileIcon;

    public RectTransform indicator;

    public float moveSpeed = 8f;
    public float iconScale = 1.2f;

    public void OpenHome()
    {
        SetScreen(homeScreen);
        SetIcons(homeIcon);
        MoveToIcon(homeIcon);
    }

    public void OpenGames()
    {
        SetScreen(gamesScreen);
        SetIcons(gamesIcon);
        MoveToIcon(gamesIcon);
    }

    public void OpenLibrary()
    {
        SetScreen(libraryScreen);
        SetIcons(libraryIcon);
        MoveToIcon(libraryIcon);
    }

    public void OpenProfile()
    {
        SetScreen(profileScreen);
        SetIcons(profileIcon);
        MoveToIcon(profileIcon);
    }

    void SetScreen(GameObject screen)
    {
        homeScreen.SetActive(false);
        gamesScreen.SetActive(false);
        libraryScreen.SetActive(false);
        profileScreen.SetActive(false);

        screen.SetActive(true);
    }

    void SetIcons(Image activeIcon)
    {
        Image[] icons = { homeIcon, gamesIcon, libraryIcon, profileIcon };

        foreach (Image icon in icons)
        {
            icon.color = Color.white;
            icon.transform.localScale = Vector3.one;
        }

        activeIcon.color = Color.gray;
        activeIcon.transform.localScale = Vector3.one * iconScale;
    }

    void MoveToIcon(Image icon)
    {
        StopAllCoroutines();
        StartCoroutine(MoveIndicator(indicator.anchoredPosition, icon.rectTransform.anchoredPosition));
    }

    IEnumerator MoveIndicator(Vector2 startPos, Vector2 targetPos)
    {
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * moveSpeed;
            indicator.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        indicator.anchoredPosition = targetPos;
    }
}