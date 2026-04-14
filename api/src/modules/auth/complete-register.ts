import { prisma } from "@/lib/db";

type CompleteRegisterUser = {
  id: string;
  name: string;
  image?: string | null;
};

export const completeRegister = async (user: CompleteRegisterUser) =>
  prisma.$transaction(async (tx) => {
    await tx.profile.upsert({
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
  });
