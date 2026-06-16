using System;

[Serializable]
public class QuizAnswer
{
    public string id;
    public string text;
    public int sortOrder;
}

[Serializable]
public class QuizQuestion
{
    public string id;
    public string question;
    public string fact;
    public int correctIndex;
    public QuizAnswer[] answers;
}