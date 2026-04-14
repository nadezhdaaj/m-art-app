using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class QuizManager : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        public string questionText;
        public List<string> answers;
        public int correctAnswerIndex;
        public string fact;
    }

    public List<Question> questions;

    public TMP_Text questionText;
    public List<TMP_Text> answerTexts;
    public List<Button> answerButtons;

    public GameObject factPanel;
    public TMP_Text factText;

    public TMP_Text progressText;
    public Image progressFill;
    public GameObject endPanel;

    public GameObject gameScreen;
    public GameObject miniGamesScreen;

    public GameObject[] covers;
    public Transform coversParent;
    private List<GameObject> remainingCovers = new List<GameObject>();

    private int currentQuestion = 0;
    private bool puzzleOpenedThisQuestion = false;

    void Start()
    {
        factPanel.SetActive(false);
        endPanel.SetActive(false);

        if (coversParent != null)
        {
            PopulateCoversFromParent();
        }

        if (covers == null)
        {
            covers = new GameObject[0];
        }

        remainingCovers.AddRange(covers);

        ShowQuestion();
    }

    void PopulateCoversFromParent()
    {
        covers = new GameObject[coversParent.childCount];
        for (int i = 0; i < coversParent.childCount; i++)
        {
            covers[i] = coversParent.GetChild(i).gameObject;
        }
    }

    void ShowQuestion()
    {
        Question q = questions[currentQuestion];

        questionText.text = q.questionText;
        UpdateProgressText();

        puzzleOpenedThisQuestion = false;

        for (int i = 0; i < answerTexts.Count; i++)
        {
            answerTexts[i].text = q.answers[i];
            answerButtons[i].interactable = true;

            int index = i;

            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => AnswerClicked(index));
        }
    }

    void AnswerClicked(int index)
    {
        Question q = questions[currentQuestion];

        if (index == q.correctAnswerIndex)
        {
            answerButtons[index].GetComponent<Image>().color = Color.green;

            if (!puzzleOpenedThisQuestion)
            {
                OpenPuzzlePiece();
                puzzleOpenedThisQuestion = true;
            }
        }
        else
        {
            answerButtons[index].GetComponent<Image>().color = Color.red;
            answerButtons[q.correctAnswerIndex].GetComponent<Image>().color = Color.green;
        }

        foreach (Button btn in answerButtons)
        {
            btn.interactable = false;
        }

        factText.text = q.fact;
        factPanel.SetActive(true);
    }

    public void NextQuestion()
    {
        factPanel.SetActive(false);

        currentQuestion++;

        if (currentQuestion < questions.Count)
        {
            ResetButtons();
            ShowQuestion();
        }
        else
        {
            ShowEndScreen();
        }
    }

    void UpdateProgressText()
    {
        int totalQuestions = Mathf.Max(1, questions.Count);
        int currentStep = Mathf.Clamp(currentQuestion + 1, 1, totalQuestions);
        int completedQuestions = Mathf.Clamp(currentQuestion, 0, totalQuestions);

        if (progressText != null)
        {
            progressText.text = $"STEP {currentStep}/{totalQuestions}";
        }

        if (progressFill != null)
        {
            progressFill.fillAmount = (float)completedQuestions / totalQuestions;
        }
    }

    void ResetButtons()
    {
        foreach (Button btn in answerButtons)
        {
            btn.GetComponent<Image>().color = Color.white;
        }
    }

    void OpenPuzzlePiece()
    {
        if (remainingCovers.Count == 0) return;

        int randomIndex = Random.Range(0, remainingCovers.Count);

        GameObject coverToOpen = remainingCovers[randomIndex];

        coverToOpen.SetActive(false);

        remainingCovers.RemoveAt(randomIndex);
    }

    void ShowEndScreen()
    {
        endPanel.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        gameScreen.SetActive(false);
        miniGamesScreen.SetActive(true);
    }
}