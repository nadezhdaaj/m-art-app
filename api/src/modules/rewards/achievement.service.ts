import type {
  AchievementRuleType,
  Prisma,
  ProgressEventSourceType,
} from "@/generated/prisma/client";
import { ProgressService } from "../progress/service";

type EvaluateQuizAchievementsInput = {
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

type UnlockQuizAchievementsInput = EvaluateQuizAchievementsInput & {
  sourceType: ProgressEventSourceType;
  sourceId: string;
};

const getNumber = (value: unknown, fallback = 0) =>
  typeof value === "number" && Number.isFinite(value) ? value : fallback;

const getString = (value: unknown) => (typeof value === "string" ? value : null);

const matchesRule = (
  achievement: {
    ruleType: AchievementRuleType;
    ruleConfig: Prisma.JsonValue;
  },
  input: EvaluateQuizAchievementsInput,
) => {
  const config =
    achievement.ruleConfig && typeof achievement.ruleConfig === "object"
      ? achievement.ruleConfig
      : {};

  switch (achievement.ruleType) {
    case "QUIZ_COMPLETIONS":
      return (
        input.profile.totalQuizCompletions >=
        getNumber((config as Record<string, unknown>).minCompletions, 1)
      );
    case "QUIZ_PERFECT_SCORE": {
      const quizSlug = getString((config as Record<string, unknown>).quizSlug);
      return (
        input.attempt.totalQuestions > 0 &&
        input.attempt.correctAnswers === input.attempt.totalQuestions &&
        (!quizSlug || quizSlug === input.quiz.slug)
      );
    }
    case "QUIZ_CORRECT_ANSWERS":
      return (
        input.profile.totalQuizCorrectAnswers >=
        getNumber((config as Record<string, unknown>).minCorrectAnswers, 1)
      );
    case "QUIZ_SINGLE_COMPLETION": {
      const quizSlug = getString((config as Record<string, unknown>).quizSlug);
      const minCorrectAnswers = getNumber(
        (config as Record<string, unknown>).minCorrectAnswers,
        input.attempt.totalQuestions,
      );

      return (
        (!quizSlug || quizSlug === input.quiz.slug) &&
        input.attempt.correctAnswers >= minCorrectAnswers
      );
    }
    default:
      return false;
  }
};

export abstract class AchievementService {
  static async evaluateQuizAchievements(input: EvaluateQuizAchievementsInput) {
    const [achievements, unlockedAchievements] = await Promise.all([
      input.tx.achievement.findMany({
        where: {
          ruleType: {
            in: [
              "QUIZ_COMPLETIONS",
              "QUIZ_PERFECT_SCORE",
              "QUIZ_CORRECT_ANSWERS",
              "QUIZ_SINGLE_COMPLETION",
            ],
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
  }

  static async unlockQuizAchievements(input: UnlockQuizAchievementsInput) {
    const achievements = await this.evaluateQuizAchievements(input);
    const unlocked = [];

    for (const achievement of achievements) {
      const userAchievement = await input.tx.userAchievement.upsert({
        where: {
          profileId_achievementId: {
            profileId: input.profile.id,
            achievementId: achievement.id,
          },
        },
        create: {
          profileId: input.profile.id,
          achievementId: achievement.id,
          progress: 100,
          metadata: {
            quizId: input.quiz.id,
            quizSlug: input.quiz.slug,
            attemptId: input.attempt.id,
          },
        },
        update: {},
        include: {
          achievement: true,
        },
      });

      await ProgressService.applyEvent({
        tx: input.tx,
        profileId: input.profile.id,
        sourceType: input.sourceType,
        sourceId: input.sourceId,
        eventType: "ACHIEVEMENT_UNLOCKED",
        xpDelta: achievement.xpReward,
        idempotencyKey: `achievement-unlock:${input.profile.id}:${achievement.code}`,
        payload: {
          achievementId: achievement.id,
          achievementCode: achievement.code,
          achievementName: achievement.name,
          xpReward: achievement.xpReward,
          quizId: input.quiz.id,
          attemptId: input.attempt.id,
        },
      });

      unlocked.push(userAchievement);
    }

    return unlocked;
  }
}
