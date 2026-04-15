import { status } from "elysia";
import { UpdateProfileBody } from "../model";
import { ERROR_CODES } from "@/lib/http";
import { uploadAvatar } from "./upload-avatar";
import { prisma } from "@/lib/db";
import { toPublicAvatarUrl } from "../mapper";

export const updateProfile = async (userId: string, body: UpdateProfileBody) => {
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

    return toPublicAvatarUrl(profile);
  });
};
