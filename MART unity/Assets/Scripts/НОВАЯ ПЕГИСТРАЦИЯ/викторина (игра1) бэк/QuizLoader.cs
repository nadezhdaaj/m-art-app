using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class QuizLoader : MonoBehaviour
{
    static string QuestionsUrl => AppConfig.BaseUrl + "/quiz/questions";

    void Start()
    {
        StartCoroutine(LoadQuestions());
    }

    IEnumerator LoadQuestions()
    {
        UnityWebRequest request = UnityWebRequest.Get(QuestionsUrl);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;

            Debug.Log("RAW JSON:");
            Debug.Log(json);

            QuizQuestion[] questions =
                JsonHelper.FromJson<QuizQuestion>(json);

            Debug.Log("QUESTIONS COUNT: " + questions.Length);
            Debug.Log("FIRST QUESTION: " + questions[0].question);
        }
        else
        {
            Debug.LogError(request.error);
        }
    }
}