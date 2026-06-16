using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Globalization;
using System.Collections;
using System.Text;
using UnityEngine.Networking;

public class QuizManager : MonoBehaviour
{
    [System.Serializable]
    public class AddPointsResponse
    {
        public string message;
        public int points;
        public string title;
    }

    [System.Serializable]
    public class Question
    {
        public string questionText;
        public List<string> answers;
        public int correctAnswerIndex;
        public string correctAnswerText;
        public string fact;
    }

    public List<Question> questions;

    [Header("Settings")]
    public int questionsPerRound = 12;

    public TMP_Text questionText;
    public List<TMP_Text> answerTexts;
    public List<Button> answerButtons;

    public GameObject factPanel;
    public TMP_Text endTitleText;
    public TMP_Text factText;

    public TMP_Text progressText;
    public Image progressFill;

    public GameObject endPanel;
    public TreasureChestReveal treasureChest;

    [Header("Puzzle")]
    public Transform puzzleCoversRoot;
    public List<GameObject> puzzleCovers = new List<GameObject>();

    public float maxQuizPoints = 15f;

    private int currentQuestion = 0;
    private int correctAnswers = 0;

    private bool questionAnswered = false;
    private List<Question> activeQuestions = new List<Question>();

    public int totalPoints = 350;

    void Start()
    {
        StartCoroutine(LoadQuestionsAndStart());
    }

    public void Restart()
    {
        StartCoroutine(LoadQuestionsAndStart());
    }

    IEnumerator LoadQuestionsAndStart()
    {
        activeQuestions = new List<Question>();

        string url = AppConfig.BaseUrl + "/quiz/questions?count=" + questionsPerRound;
        using (var req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var apiQuestions = JsonHelper.FromJson<QuizQuestion>(req.downloadHandler.text);
                activeQuestions = MapQuestions(apiQuestions);
            }
            else
            {
                Debug.LogWarning("Quiz: не удалось загрузить вопросы с сервера — " + req.error);
            }
        }

        if (activeQuestions.Count == 0)
            activeQuestions = BuildFallbackQuestions();

        ResetQuiz();
    }

    List<Question> MapQuestions(QuizQuestion[] apiQuestions)
    {
        var list = new List<Question>();
        if (apiQuestions == null)
            return list;

        foreach (var q in apiQuestions)
        {
            if (q == null || q.answers == null || q.answers.Length < 4)
                continue;

            var sortedAnswers = new List<QuizAnswer>(q.answers);
            sortedAnswers.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));

            var answers = new List<string>();
            for (int i = 0; i < sortedAnswers.Count; i++)
                answers.Add(sortedAnswers[i].text);

            int correctIndex = q.correctIndex;
            if (correctIndex < 0 || correctIndex >= answers.Count)
                continue;

            string correctText = answers[correctIndex];
            ShuffleList(answers);
            correctIndex = answers.IndexOf(correctText);
            if (correctIndex < 0)
                continue;

            list.Add(new Question
            {
                questionText = q.question,
                answers = answers,
                correctAnswerIndex = correctIndex,
                correctAnswerText = correctText,
                fact = q.fact
            });
        }

        return list;
    }

    int GetCorrectAnswerIndex(Question q)
    {
        if (q == null || q.answers == null || q.answers.Count == 0)
            return -1;

        if (!string.IsNullOrEmpty(q.correctAnswerText))
        {
            for (int i = 0; i < q.answers.Count; i++)
            {
                if (q.answers[i] == q.correctAnswerText)
                    return i;
            }
        }

        if (q.correctAnswerIndex >= 0 && q.correctAnswerIndex < q.answers.Count)
            return q.correctAnswerIndex;

        return -1;
    }

    void EnsureCorrectAnswerText(Question q)
    {
        if (q == null || q.answers == null || q.answers.Count == 0)
            return;

        if (!string.IsNullOrEmpty(q.correctAnswerText))
            return;

        if (q.correctAnswerIndex >= 0 && q.correctAnswerIndex < q.answers.Count)
            q.correctAnswerText = q.answers[q.correctAnswerIndex];
    }

    List<Question> BuildFallbackQuestions()
    {
        if (questions == null || questions.Count == 0)
            return new List<Question>();

        var copy = new List<Question>();
        for (int i = 0; i < questions.Count; i++)
        {
            var source = questions[i];
            if (source == null || source.answers == null || source.answers.Count < 4)
                continue;

            EnsureCorrectAnswerText(source);

            var answers = new List<string>(source.answers);
            string correctText = source.correctAnswerText;
            ShuffleList(answers);

            int correctIndex = answers.IndexOf(correctText);
            if (correctIndex < 0)
                correctIndex = source.correctAnswerIndex;

            copy.Add(new Question
            {
                questionText = source.questionText,
                answers = answers,
                correctAnswerIndex = correctIndex,
                correctAnswerText = correctText,
                fact = source.fact
            });
        }

        ShuffleList(copy);

        int take = Mathf.Min(questionsPerRound, copy.Count);
        return copy.GetRange(0, take);
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    List<Question> GetActiveQuestions()
    {
        return activeQuestions != null && activeQuestions.Count > 0
            ? activeQuestions
            : questions;
    }

    void ResetQuiz()
    {
        currentQuestion = 0;
        correctAnswers = 0;

        if (endPanel != null)
            endPanel.SetActive(false);

        if (factPanel != null)
            factPanel.SetActive(false);

        if (treasureChest != null)
            treasureChest.HidePanel();

        ResetPuzzleCovers();
        ShowQuestion();
    }

    void EnsurePuzzleCoversLoaded()
    {
        if (puzzleCovers.Count > 0 || puzzleCoversRoot == null)
            return;

        for (int i = 0; i < puzzleCoversRoot.childCount; i++)
            puzzleCovers.Add(puzzleCoversRoot.GetChild(i).gameObject);
    }

    void ResetPuzzleCovers()
    {
        EnsurePuzzleCoversLoaded();

        for (int i = 0; i < puzzleCovers.Count; i++)
        {
            if (puzzleCovers[i] != null)
                puzzleCovers[i].SetActive(true);
        }
    }

    void RevealPuzzlePiece(int questionIndex)
    {
        EnsurePuzzleCoversLoaded();

        if (questionIndex < 0 || questionIndex >= puzzleCovers.Count)
            return;

        if (puzzleCovers[questionIndex] != null)
            puzzleCovers[questionIndex].SetActive(false);
    }

    public void AnswerClicked(int index)
    {
        Answer(index);
    }

    void ShowQuestion()
    {
        var roundQuestions = GetActiveQuestions();

        if (roundQuestions == null || roundQuestions.Count == 0 || currentQuestion >= roundQuestions.Count)
            return;

        if (questionText == null || answerButtons == null || answerButtons.Count == 0)
            return;

        var q = roundQuestions[currentQuestion];

        questionText.text = q.questionText;

        questionAnswered = false;

        for (int i = 0; i < answerButtons.Count; i++)
        {
            int index = i;

            if (answerTexts != null && i < answerTexts.Count && answerTexts[i] != null
                && q.answers != null && i < q.answers.Count)
            {
                answerTexts[i].text = q.answers[i];
            }

            if (answerButtons[i] == null)
                continue;

            answerButtons[i].interactable = true;
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => Answer(index));

            var buttonImage = answerButtons[i].GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.color = Color.white;
        }

        UpdateProgress();
    }

    void Answer(int index)
    {
        var roundQuestions = GetActiveQuestions();

        if (questionAnswered || roundQuestions == null || currentQuestion >= roundQuestions.Count)
            return;

        if (answerButtons == null || index < 0 || index >= answerButtons.Count)
            return;

        questionAnswered = true;

        var q = roundQuestions[currentQuestion];
        EnsureCorrectAnswerText(q);

        int correctIndex = GetCorrectAnswerIndex(q);
        if (correctIndex < 0)
            return;

        bool isCorrect = index == correctIndex;

        if (isCorrect)
        {
            correctAnswers++;
            var correctImage = answerButtons[index].GetComponent<Image>();
            if (correctImage != null)
                correctImage.color = Color.green;
            RevealPuzzlePiece(currentQuestion);
            UpdateProgress();
        }
        else
        {
            var wrongImage = answerButtons[index].GetComponent<Image>();
            if (wrongImage != null)
                wrongImage.color = Color.red;

            var correctImage = answerButtons[correctIndex].GetComponent<Image>();
            if (correctImage != null)
                correctImage.color = Color.green;
        }

        for (int i = 0; i < answerButtons.Count; i++)
        {
            if (answerButtons[i] != null)
                answerButtons[i].interactable = false;
        }

        if (factText != null)
            factText.text = q.fact;

        if (factPanel != null)
            factPanel.SetActive(true);
    }

    // ✅ ВАЖНО: ЭТО НЕ ТРОГАЕМ — это твоя кнопка Next
    public void Next()
    {
        if (!questionAnswered)
            return;

        if (factPanel != null)
            factPanel.SetActive(false);

        currentQuestion++;

        var roundQuestions = GetActiveQuestions();

        if (currentQuestion < roundQuestions.Count)
        {
            ShowQuestion();
        }
        else
        {
            Finish();
        }
    }

    void Finish()
    {
        float points = CalculatePoints(correctAnswers);
        var roundQuestions = GetActiveQuestions();

        SendToServer(points);

        if (progressFill != null)
            progressFill.fillAmount = 1f;

        if (endTitleText != null)
        {
            endTitleText.text =
                "Игра завершена!\nТы набрал: "
                + Mathf.RoundToInt(points)
                + " очков";
        }

        if (treasureChest != null && roundQuestions != null)
        {
            treasureChest.Show(correctAnswers, roundQuestions.Count, points);
            return;
        }

        if (endPanel != null)
            endPanel.SetActive(true);
    }

    float CalculatePoints(int correct)
    {
        var roundQuestions = GetActiveQuestions();
        if (roundQuestions == null || roundQuestions.Count == 0)
            return 0f;

        return maxQuizPoints * correct / roundQuestions.Count;
    }

    void UpdateProgress()
    {
        var roundQuestions = GetActiveQuestions();
        int total = roundQuestions != null ? roundQuestions.Count : 0;
        if (total <= 0)
            return;

        if (progressFill != null)
            progressFill.fillAmount = (float)(currentQuestion + 1) / total;

        if (progressText != null)
            progressText.text = "STEP " + (currentQuestion + 1) + "/" + total;
    }

    void SendToServer(float points)
    {
        StartCoroutine(Send(points));
    }

    IEnumerator Send(float points)
    {
        if (UserSession.Instance == null)
            yield break;

        string url = AppConfig.BaseUrl + "/profile/" + UserSession.Instance.UserId + "/add-points";

        string json = "{\"points\":" + Mathf.RoundToInt(points) + "}";

        var req = new UnityWebRequest(url, "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);

        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<AddPointsResponse>(req.downloadHandler.text);

            UserSession.Instance.Points = res.points;
            UserSession.Instance.Title = res.title;

            if (ProfileUI.Instance != null)
                ProfileUI.Instance.RefreshProfileData();
        }
    }

    public void GoToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("The main stage");
    }
}