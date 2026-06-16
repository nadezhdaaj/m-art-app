using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class QuizGame : MonoBehaviour
{
    static string QuestionsUrl => AppConfig.BaseUrl + "/quiz/questions";

    public TMPro.TextMeshProUGUI questionText;
    public TMPro.TextMeshProUGUI factText;
    public TMPro.TextMeshProUGUI[] answerTexts;

    QuizQuestion[] questions;
    int current = 0;

    void Start()
    {
        StartCoroutine(Load());
    }

    IEnumerator Load()
    {
        UnityWebRequest req = UnityWebRequest.Get(QuestionsUrl);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            questions = JsonHelper.FromJson<QuizQuestion>(req.downloadHandler.text);
            Show();
        }
        else
        {
            Debug.LogError(req.error);
        }
    }

    void Show()
    {
        var q = questions[current];

        questionText.text = q.question;
        factText.text = "";

        for (int i = 0; i < answerTexts.Length; i++)
        {
            answerTexts[i].text = q.answers[i].text;
        }
    }

    public void Answer(int index)
    {
        var q = questions[current];

        if (index == q.correctIndex)
        {
            Debug.Log("RIGHT");
        }
        else
        {
            Debug.Log("WRONG");
        }

        factText.text = q.fact;

        current++;

        if (current < questions.Length)
        {
            Invoke("Show", 2f);
        }
        else
        {
            questionText.text = "GAME OVER";
        }
    }
}