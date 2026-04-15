import { prisma } from "@/lib/db";

type SetupUserProfile = {
  id: string;
  name: string;
  image?: string | null;
};

export const setupUserProfile = async (user: SetupUserProfile) => {
  await prisma.profile.upsert({
    where: {
      userId: user.id,
    },
    create: {
      userId: user.id,
      displayName: user.name,
      avatarUrl: user.image,
      xp: 0,
    },
    update: {},
  });
};
