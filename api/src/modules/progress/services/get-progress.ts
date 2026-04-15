import { prisma } from "@/lib/db";
import { getProfile } from "@/modules/profile/services";
import { getLevelProgress } from "../level";
import { mapAchievementSummary } from "../achievement.mapper";

export const getProgress = async (userId: string) => {
  const profile = await getProfile(userId);

  const [achievements, totalQuizCompletions, totalQuizCorrectAnswers, lastRewardEvent] =
    await Promise.all([
      prisma.userAchievement.findMany({
        where: { profileId: profile.id },
        include: {
          achievement: true,
        },
        orderBy: { unlockedAt: "desc" },
      }),
      prisma.quizAttempt.count({
        where: {
          profileId: profile.id,
          status: "COMPLETED",
        },
      }),
      prisma.quizAttempt.aggregate({
        where: {
          profileId: profile.id,
          status: "COMPLETED",
        },
        _sum: {
          correctAnswers: true,
        },
      }),
      prisma.progressEvent.findFirst({
        where: { profileId: profile.id },
        orderBy: { createdAt: "desc" },
      }),
    ]);

  return {
    xp: profile.xp,
    totalQuizCompletions,
    totalQuizCorrectAnswers: totalQuizCorrectAnswers._sum.correctAnswers ?? 0,
    lastRewardedAt: lastRewardEvent?.createdAt.toISOString() ?? null,
    levelProgress: getLevelProgress(profile.xp),
    achievements: achievements.map(mapAchievementSummary),
  };
};
