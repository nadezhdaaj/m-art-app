import type { Prisma } from "@/generated/prisma/client";
import { getLevelByXp } from "../progress/level";
import type { RewardSummary } from "../progress/model";
import { ProgressService } from "../progress/service";
import { AchievementService } from "./achievement.service";
import { calculateQuizCompletionXp } from "./rules/quiz";

type GrantQuizCompletionRewardsInput = {
  tx: Prisma.TransactionClient;
  profileId: string;
  quizId: string;
  attemptId: string;
  totalQuestions: number;
  correctAnswers: number;
};

const mapAchievementSummary = (achievement: {
  unlockedAt: Date;
  progress: number;
  achievement: {
    id: string;
    code: string;
    name: string;
    description: string | null;
    iconUrl: string | null;
    category: string | null;
    rarity: string | null;
  };
}): RewardSummary["unlockedAchievements"][number] => ({
  id: achievement.achievement.id,
  code: achievement.achievement.code,
  name: achievement.achievement.name,
  description: achievement.achievement.description,
  iconUrl: achievement.achievement.iconUrl,
  category: achievement.achievement.category,
  rarity: achievement.achievement.rarity,
  unlockedAt: achievement.unlockedAt.toISOString(),
  progress: achievement.progress,
});

export abstract class RewardService {
  static async grantQuizCompletionRewards(
    input: GrantQuizCompletionRewardsInput,
  ): Promise<RewardSummary> {
    const [profile, quiz, previousQuizCompletions, previousCorrectAnswersAggregate] =
      await Promise.all([
        input.tx.profile.findUniqueOrThrow({
          where: { id: input.profileId },
        }),
        input.tx.quiz.findUniqueOrThrow({
          where: { id: input.quizId },
          select: { id: true, slug: true },
        }),
        input.tx.quizAttempt.count({
          where: {
            profileId: input.profileId,
            quizId: input.quizId,
            status: "COMPLETED",
            id: { not: input.attemptId },
          },
        }),
        input.tx.quizAttempt.aggregate({
          where: {
            profileId: input.profileId,
            status: "COMPLETED",
            id: { not: input.attemptId },
          },
          _sum: {
            correctAnswers: true,
          },
        }),
      ]);

    const previousLevel = getLevelByXp(profile.xp);
    const previousXp = profile.xp;
    const isFirstCompletion = previousQuizCompletions === 0;
    const xpReward = calculateQuizCompletionXp({
      correctAnswers: input.correctAnswers,
      totalQuestions: input.totalQuestions,
      isFirstCompletion,
    });

    const quizEvent = await ProgressService.applyEvent({
      tx: input.tx,
      profileId: input.profileId,
      sourceType: "QUIZ_ATTEMPT",
      sourceId: input.attemptId,
      eventType: "QUIZ_COMPLETED",
      xpDelta: xpReward.gainedXp,
      idempotencyKey: `quiz-completion:${input.profileId}:${input.attemptId}`,
      payload: {
        quizId: input.quizId,
        attemptId: input.attemptId,
        previousXp,
        previousLevel,
        correctAnswers: input.correctAnswers,
        totalQuestions: input.totalQuestions,
        ...xpReward.breakdown,
      },
    });

    const achievementProfile = {
      id: profile.id,
      totalQuizCompletions: previousQuizCompletions + (quizEvent.applied ? 1 : 0),
      totalQuizCorrectAnswers:
        (previousCorrectAnswersAggregate._sum.correctAnswers ?? 0) +
        (quizEvent.applied ? input.correctAnswers : 0),
    };

    const unlockedAchievements = await AchievementService.unlockQuizAchievements({
      tx: input.tx,
      profile: achievementProfile,
      quiz,
      attempt: {
        id: input.attemptId,
        correctAnswers: input.correctAnswers,
        totalQuestions: input.totalQuestions,
      },
      sourceType: "QUIZ_ATTEMPT",
      sourceId: input.attemptId,
    });

    const achievementXp = unlockedAchievements.reduce(
      (total, achievement) => total + achievement.achievement.xpReward,
      0,
    );

    const totalXp = previousXp + xpReward.gainedXp + achievementXp;
    const currentLevel = getLevelByXp(totalXp);

    return {
      gainedXp: xpReward.gainedXp + achievementXp,
      totalXp,
      previousLevel,
      currentLevel,
      leveledUp: currentLevel > previousLevel,
      unlockedAchievements: unlockedAchievements.map(mapAchievementSummary),
    };
  }
}
