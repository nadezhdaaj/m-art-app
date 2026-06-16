using TMPro;
using UnityEngine;

public class AuthUIManager : MonoBehaviour
{
    public static AuthUIManager instance;

    [Header("References")]
    [SerializeField] private GameObject checkigForAccountUI;
    [SerializeField] private GameObject loginUI;
    [SerializeField] private GameObject registerUI;
    [SerializeField] private GameObject verifyEmailUI;
    [SerializeField] private TMP_Text verifyEmailText;
    [SerializeField] private GameObject welcomeUI;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (welcomeUI == null)
        {
            welcomeUI = FindPanel("WelcomePanel");
        }

        if (loginUI == null)
        {
            loginUI = FindPanel("LoginPanel");
        }

        if (registerUI == null)
        {
            registerUI = FindPanel("RegisterPanel");
        }

        if (checkigForAccountUI == null)
        {
            checkigForAccountUI = welcomeUI;
        }

        if (verifyEmailUI == null)
        {
            verifyEmailUI = FindPanel("VerifyEmailPanel");
        }

        if (verifyEmailText == null && verifyEmailUI != null)
        {
            verifyEmailText = verifyEmailUI.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private static GameObject FindPanel(string panelName)
    {
        GameObject panel = GameObject.Find(panelName);
        return panel;
    }

    private void ClearUI()
    {
        if (loginUI != null)
        {
            loginUI.SetActive(false);
        }

        if (registerUI != null)
        {
            registerUI.SetActive(false);
        }

        if (verifyEmailUI != null)
        {
            verifyEmailUI.SetActive(false);
        }

        if (checkigForAccountUI != null)
        {
            checkigForAccountUI.SetActive(false);
        }

        if (welcomeUI != null)
        {
            welcomeUI.SetActive(false);
        }

        if (BackendManager.instance != null)
        {
            BackendManager.instance.ClearOutputs();
        }
    }

    public void CheckingForAccountScreen()
    {
        ClearUI();

        if (checkigForAccountUI != null)
        {
            checkigForAccountUI.SetActive(true);
        }
    }

    public void LoginScreen()
    {
        ClearUI();

        if (loginUI != null)
        {
            loginUI.SetActive(true);
        }
    }

    public void RegisterScreen()
    {
        ClearUI();

        if (registerUI != null)
        {
            registerUI.SetActive(true);
        }
    }

    public void AwaitVerification(bool emailSent, string email, string output)
    {
        ClearUI();

        if (verifyEmailUI != null)
        {
            verifyEmailUI.SetActive(true);
        }

        if (verifyEmailText != null)
        {
            if (emailSent)
            {
                verifyEmailText.text = $"Письмо отправлено.\nПроверьте адрес {email}";
            }
            else
            {
                verifyEmailText.text = $"Не удалось отправить письмо: {output}\nПроверьте адрес {email}";
            }
        }
    }

    public void WelcomeScreen()
    {
        ClearUI();

        if (welcomeUI != null)
        {
            welcomeUI.SetActive(true);
        }
        else if (loginUI != null)
        {
            loginUI.SetActive(true);
        }
    }
}
