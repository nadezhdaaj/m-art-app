import type { AchievementRuleType, Prisma } from "@/generated/prisma/client";

const QUIZ_ACHIEVEMENT_RULE_TYPES = [
  "QUIZ_COMPLETIONS",
  "QUIZ_PERFECT_SCORE",
  "QUIZ_CORRECT_ANSWERS",
  "QUIZ_SINGLE_COMPLETION",
] as const satisfies AchievementRuleType[];

export type EvaluateQuizAchievementsInput = {
  tx: Prisma.TransactionClient;
  profile: {
    id: string;
    totalQuizCompletions: number;
    totalQuizCorrectAnswers: number;
  };
  quiz: {
    id: string;
    slug: string;
  };
  attempt: {
    id: string;
    correctAnswers: number;
    totalQuestions: number;
  };
};

const getNumber = (value: unknown, fallback = 0) =>
  typeof value === "number" && Number.isFinite(value) ? value : fallback;

const getString = (value: unknown) => (typeof value === "string" ? value : null);

const getRuleConfig = (ruleConfig: Prisma.JsonValue) =>
  ruleConfig && typeof ruleConfig === "object" && !Array.isArray(ruleConfig)
    ? (ruleConfig as Record<string, unknown>)
    : {};

const matchesRule = (
  achievement: {
    ruleType: AchievementRuleType;
    ruleConfig: Prisma.JsonValue;
  },
  input: EvaluateQuizAchievementsInput,
) => {
  const config = getRuleConfig(achievement.ruleConfig);

  switch (achievement.ruleType) {
    case "QUIZ_COMPLETIONS":
      return input.profile.totalQuizCompletions >= getNumber(config.minCompletions, 1);
    case "QUIZ_PERFECT_SCORE": {
      const quizSlug = getString(config.quizSlug);
      return (
        input.attempt.totalQuestions > 0 &&
        input.attempt.correctAnswers === input.attempt.totalQuestions &&
        (!quizSlug || quizSlug === input.quiz.slug)
      );
    }
    case "QUIZ_CORRECT_ANSWERS":
      return input.profile.totalQuizCorrectAnswers >= getNumber(config.minCorrectAnswers, 1);
    case "QUIZ_SINGLE_COMPLETION": {
      const quizSlug = getString(config.quizSlug);
      const minCorrectAnswers = getNumber(config.minCorrectAnswers, input.attempt.totalQuestions);

      return (
        (!quizSlug || quizSlug === input.quiz.slug) &&
        input.attempt.correctAnswers >= minCorrectAnswers
      );
    }
    default:
      return false;
  }
};

export const evaluateQuizAchievements = async (input: EvaluateQuizAchievementsInput) => {
  const [achievements, unlockedAchievements] = await Promise.all([
    input.tx.achievement.findMany({
      where: {
        ruleType: {
          in: QUIZ_ACHIEVEMENT_RULE_TYPES,
        },
      },
    }),
    input.tx.userAchievement.findMany({
      where: { profileId: input.profile.id },
      select: { achievementId: true },
    }),
  ]);

  const unlockedIds = new Set(unlockedAchievements.map((entry) => entry.achievementId));

  return achievements.filter(
    (achievement) => !unlockedIds.has(achievement.id) && matchesRule(achievement, input),
  );
};
