import { status } from "elysia";
import type { Prisma } from "@/generated/prisma/client";
import { prisma } from "@/lib/db";
import { ERROR_CODES } from "@/lib/http";
import { s3Storage } from "@/lib/s3";
import { uploadAvatar } from "./avatar";
import { ProfileModels, UpdateProfileBody } from "./model";

const profileInclude = {} as const;

const withAvatarUrl = <T extends { avatarUrl: string | null }>(profile: T) => ({
  ...profile,
  avatarUrl: s3Storage.resolvePublicUrl(profile.avatarUrl),
});

type DbClient = typeof prisma | Prisma.TransactionClient;

export abstract class ProfileService {
  static async getProfile(userId: string, client: DbClient = prisma) {
    const profile = await client.profile.findUnique({
      where: { userId },
      include: profileInclude,
    });

    if (!profile) {
      throw status(404, {
        code: ERROR_CODES.PROFILE_NOT_FOUND,
        message: "Profile was not found for the current user.",
      });
    }

    return withAvatarUrl(profile);
  }

  static async updateProfile(userId: string, body: ProfileModels["updateProfileBody"]) {
    if (Object.keys(body).length === 0) {
      throw status(400, {
        code: ERROR_CODES.PROFILE_UPDATE_EMPTY,
        message: "At least one profile field must be provided.",
      });
    }

    const { avatar, ...profileInput } = body;
    const data: { displayName?: string | null; avatarUrl?: string | null } = {
      ...profileInput,
    };

    if (avatar !== undefined) {
      data.avatarUrl = await uploadAvatar(userId, avatar);
    }

    if (Object.keys(data).length === 0) {
      throw status(400, {
        code: ERROR_CODES.PROFILE_UPDATE_EMPTY,
        message: "At least one profile field must be provided.",
      });
    }

    return prisma.$transaction(async (tx) => {
      const profile = await tx.profile.upsert({
        where: { userId },
        create: {
          userId,
          xp: 0,
          ...data,
        },
        update: data,
      });

      const updatedProfile = await tx.profile.findUniqueOrThrow({
        where: { userId },
        include: profileInclude,
      });

      return withAvatarUrl(updatedProfile);
    });
  }
}
