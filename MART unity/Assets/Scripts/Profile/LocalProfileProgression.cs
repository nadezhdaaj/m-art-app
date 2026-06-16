using System;
using UnityEngine;

public static class LocalProfileProgression
{
    private const string QuizPointsKey = "QuizManager.LastCompletedPoints";
    private const string QuizCompletedKey = "QuizManager.LastCompletedTotalQuestions";
    private const string PaintPointsKey = "LiveContourMiniGame.LastCompletedPoints";
    private const string PaintCompletedKey = "LiveContourMiniGame.HasCompletedSession";

    public readonly struct Tier
    {
        public Tier(string title, float minPoints, float maxPoints)
        {
            Title = title;
            MinPoints = minPoints;
            MaxPoints = maxPoints;
        }

        public string Title { get; }
        public float MinPoints { get; }
        public float MaxPoints { get; }
    }

    public readonly struct ProgressState
    {
        public ProgressState(float totalPoints, string currentTitle)
        {
            TotalPoints = totalPoints;
            CurrentTitle = currentTitle;
        }

        public float TotalPoints { get; }
        public string CurrentTitle { get; }
    }

    public static event Action Changed;

    private static readonly Tier[] Tiers =
    {
        new Tier("Новичок", 1f, 150f),
        new Tier("Знаток", 151f, 200f),
        new Tier("Искусствовед", 201f, 250f),
        new Tier("Мастер галереи", 251f, 310f)
    };

    public static ProgressState GetProgressState()
    {
        float totalPoints = Mathf.Max(0f, GetTotalPoints());
        return new ProgressState(totalPoints, ResolveTitle(totalPoints));
    }

    public static float GetTotalPoints()
    {
        float quizPoints = PlayerPrefs.HasKey(QuizCompletedKey)
            ? PlayerPrefs.GetFloat(QuizPointsKey, 0f)
            : 0f;

        float paintPoints = PlayerPrefs.GetInt(PaintCompletedKey, 0) == 1
            ? PlayerPrefs.GetFloat(PaintPointsKey, 0f)
            : 0f;

        return quizPoints + paintPoints;
    }

    public static void NotifyChanged()
    {
        Changed?.Invoke();
    }

    private static string ResolveTitle(float totalPoints)
    {
        if (totalPoints < Tiers[0].MinPoints)
        {
            return "Пока без титула";
        }

        for (int i = 0; i < Tiers.Length; i++)
        {
            if (totalPoints >= Tiers[i].MinPoints && totalPoints <= Tiers[i].MaxPoints)
            {
                return Tiers[i].Title;
            }
        }

        return Tiers[Tiers.Length - 1].Title;
    }
}
