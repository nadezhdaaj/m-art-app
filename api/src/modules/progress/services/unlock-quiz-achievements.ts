import type { ProgressEventSourceType } from "@/generated/prisma/client";
import { evaluateQuizAchievements, type EvaluateQuizAchievementsInput } from "./evaluate-quiz-achievements";
import { applyEvent } from "./apply-event";

export type UnlockQuizAchievementsInput = EvaluateQuizAchievementsInput & {
  sourceType: ProgressEventSourceType;
  sourceId: string;
};

export const unlockQuizAchievements = async (input: UnlockQuizAchievementsInput) => {
  const achievements = await evaluateQuizAchievements(input);
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

    await applyEvent({
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
};
