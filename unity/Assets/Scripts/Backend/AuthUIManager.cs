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
        }
    }

    private void ClearUI()
    {
        loginUI.SetActive(false);
        registerUI.SetActive(false);
        verifyEmailUI.SetActive(false);
        checkigForAccountUI.SetActive(false);
        welcomeUI.SetActive(false);

        if (BackendManager.instance != null)
        {
            BackendManager.instance.ClearOutputs();
        }
    }

    public void CheckingForAccountScreen()
    {
        ClearUI();
        checkigForAccountUI.SetActive(true);
    }

    public void LoginScreen()
    {
        ClearUI();
        loginUI.SetActive(true);
    }

    public void RegisterScreen()
    {
        ClearUI();
        registerUI.SetActive(true);
    }

    public void AwaitVerification(bool emailSent, string email, string output)
    {
        ClearUI();
        verifyEmailUI.SetActive(true);
        if (emailSent)
        {
            verifyEmailText.text = $"Письмо отправлено.\nПроверьте адрес {email}";
        }
        else
        {
            verifyEmailText.text = $"Не удалось отправить письмо: {output}\nПроверьте адрес {email}";
        }
    }

    public void WelcomeScreen()
    {
        ClearUI();
        welcomeUI.SetActive(true);
    }
}
