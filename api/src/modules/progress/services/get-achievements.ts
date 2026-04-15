import { prisma } from "@/lib/db";
import { getProfile } from "@/modules/profile/services";
import { mapAchievementSummary } from "../achievement.mapper";

export const getAchievements = async (userId: string) => {
  const profile = await getProfile(userId);

  const achievements = await prisma.userAchievement.findMany({
    where: { profileId: profile.id },
    include: {
      achievement: true,
    },
    orderBy: { unlockedAt: "desc" },
  });

  return achievements.map(mapAchievementSummary);
};
