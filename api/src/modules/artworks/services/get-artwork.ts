import { status } from "elysia";
import { prisma } from "@/lib/db";
import { ERROR_CODES } from "@/lib/http";
import { mapArtwork } from "../mapper";

export const getArtwork = async (userId: string, artworkId: string) => {
  const artwork = await prisma.artwork.findFirst({
    where: {
      id: artworkId,
      profile: {
        userId,
      },
    },
  });

  if (!artwork) {
    throw status(404, {
      code: ERROR_CODES.ARTWORK_NOT_FOUND,
      message: "Artwork was not found for the current user.",
    });
  }

  return mapArtwork(artwork);
};
