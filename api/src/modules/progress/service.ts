import type { Prisma, ProgressEventSourceType, ProgressEventType } from "@/generated/prisma/client";
import { prisma } from "@/lib/db";
import { s3Storage } from "@/lib/s3";
import { ProfileService } from "../profile/service";
import { getLevelByXp, getLevelProgress } from "./level";
import type { AchievementSummary, ProgressResponse } from "./model";

type DbClient = typeof prisma | Prisma.TransactionClient;

type ApplyEventInput = {
  tx: Prisma.TransactionClient;
  profileId: string;
  sourceType: ProgressEventSourceType;
  sourceId: string;
  eventType: ProgressEventType;
  xpDelta: number;
  idempotencyKey: string;
  payload?: Prisma.InputJsonValue;
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
}): AchievementSummary => ({
  id: achievement.achievement.id,
  code: achievement.achievement.code,
  name: achievement.achievement.name,
  description: achievement.achievement.description,
  iconUrl: s3Storage.resolvePublicUrl(achievement.achievement.iconUrl),
  category: achievement.achievement.category,
  rarity: achievement.achievement.rarity,
  unlockedAt: achievement.unlockedAt.toISOString(),
  progress: achievement.progress,
});

export abstract class ProgressService {
  static async applyEvent(input: ApplyEventInput) {
    const existingEvent = await input.tx.progressEvent.findUnique({
      where: { idempotencyKey: input.idempotencyKey },
    });

    if (existingEvent) {
      const profile = await input.tx.profile.findUniqueOrThrow({
        where: { id: input.profileId },
      });

      return {
        applied: false,
        event: existingEvent,
        profile,
      };
    }

    const currentProfile = await input.tx.profile.findUniqueOrThrow({
      where: { id: input.profileId },
    });

    const nextXp = currentProfile.xp + input.xpDelta;

    const event = await input.tx.progressEvent.create({
      data: {
        profileId: input.profileId,
        sourceType: input.sourceType,
        sourceId: input.sourceId,
        eventType: input.eventType,
        xpDelta: input.xpDelta,
        payload: input.payload,
        idempotencyKey: input.idempotencyKey,
      },
    });

    const profile = await input.tx.profile.update({
      where: { id: input.profileId },
      data: {
        xp: nextXp,
      },
    });

    return {
      applied: true,
      event,
      profile,
    };
  }

  static async getProgress(userId: string, client: DbClient = prisma): Promise<ProgressResponse> {
    const profile = await ProfileService.getProfile(userId, client);

    const [achievements, totalQuizCompletions, totalQuizCorrectAnswers, lastRewardEvent] =
      await Promise.all([
        client.userAchievement.findMany({
          where: { profileId: profile.id },
          include: {
            achievement: true,
          },
          orderBy: { unlockedAt: "desc" },
        }),
        client.quizAttempt.count({
          where: {
            profileId: profile.id,
            status: "COMPLETED",
          },
        }),
        client.quizAttempt.aggregate({
          where: {
            profileId: profile.id,
            status: "COMPLETED",
          },
          _sum: {
            correctAnswers: true,
          },
        }),
        client.progressEvent.findFirst({
          where: { profileId: profile.id },
          orderBy: { createdAt: "desc" },
        }),
      ]);

    const level = getLevelByXp(profile.xp);

    return {
      xp: profile.xp,
      totalQuizCompletions,
      totalQuizCorrectAnswers: totalQuizCorrectAnswers._sum.correctAnswers ?? 0,
      lastRewardedAt: lastRewardEvent?.createdAt.toISOString() ?? null,
      levelProgress: getLevelProgress(profile.xp),
      achievements: achievements.map(mapAchievementSummary),
    };
  }

  static async getAchievements(userId: string, client: DbClient = prisma) {
    const profile = await ProfileService.getProfile(userId, client);

    const achievements = await client.userAchievement.findMany({
      where: { profileId: profile.id },
      include: {
        achievement: true,
      },
      orderBy: { unlockedAt: "desc" },
    });

    return achievements.map(mapAchievementSummary);
  }
}
