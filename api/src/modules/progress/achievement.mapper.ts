import { s3Storage } from "@/lib/s3";
import type { AchievementSummary } from "./model";

type AchievementWithDetails = {
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
};

export const mapAchievementSummary = (achievement: AchievementWithDetails): AchievementSummary => ({
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

export const getAchievementIdsFromEvents = (events: Array<{ payload: unknown }>): string[] => {
  const achievementIds = new Set<string>();

  for (const event of events) {
    if (!event.payload || typeof event.payload !== "object" || Array.isArray(event.payload)) {
      continue;
    }

    const achievementId = (event.payload as Record<string, unknown>).achievementId;
    if (typeof achievementId === "string" && achievementId.length > 0) {
      achievementIds.add(achievementId);
    }
  }

  return [...achievementIds];
};
