import { ERROR_CODES } from "@/lib/http";
import { status } from "elysia";
import { toPublicAvatarUrl } from "../mapper";
import { prisma } from "@/lib/db";

export const getProfile = async (userId: string) => {
  const profile = await prisma.profile.findUnique({
    where: { userId },
  });

  if (!profile) {
    throw status(404, {
      code: ERROR_CODES.PROFILE_NOT_FOUND,
      message: "Profile was not found for the current user.",
    });
  }

  return toPublicAvatarUrl(profile);
};
