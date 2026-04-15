import { status } from "elysia";
import { Prisma } from "@/generated/prisma/client";
import { prisma } from "@/lib/db";
import { ERROR_CODES } from "@/lib/http";
import { mapArtwork } from "../mapper";
import type { UpdateArtworkBody } from "../model";
import { parseOptionalInteger, parseOptionalJson } from "./shared";
import { uploadArtworkMedia } from "./upload-artwork-media";

export const updateArtwork = async (userId: string, artworkId: string, body: UpdateArtworkBody) => {
  const existingArtwork = await prisma.artwork.findFirst({
    where: {
      id: artworkId,
      profile: {
        userId,
      },
    },
    include: {
      profile: true,
    },
  });

  if (!existingArtwork) {
    throw status(404, {
      code: ERROR_CODES.ARTWORK_NOT_FOUND,
      message: "Artwork was not found for the current user.",
    });
  }

  const hasUpdates =
    Object.keys(body).length > 0 &&
    Object.values(body).some((value) => value !== undefined);

  if (!hasUpdates) {
    throw status(400, {
      code: ERROR_CODES.ARTWORK_UPDATE_EMPTY,
      message: "At least one artwork field must be provided.",
    });
  }

  const metadata = parseOptionalJson("metadataJson", body.metadataJson);
  const analysis = parseOptionalJson("analysisJson", body.analysisJson);
  const schemaVersion = parseOptionalInteger("schemaVersion", body.schemaVersion);
  const kind = body.kind?.trim();

  if (body.kind !== undefined && !kind) {
    throw status(400, {
      code: ERROR_CODES.VALIDATION_FAILED,
      message: 'Field "kind" must not be empty.',
    });
  }

  const [imageUrl, thumbnailUrl] = await Promise.all([
    body.image
      ? uploadArtworkMedia({
          userId,
          artworkId,
          file: body.image,
          fieldName: "image",
        })
      : Promise.resolve<string | undefined>(undefined),
    body.thumbnail
      ? uploadArtworkMedia({
          userId,
          artworkId,
          file: body.thumbnail,
          fieldName: "thumbnail",
        })
      : Promise.resolve<string | undefined>(undefined),
  ]);

  const nextStatus = body.status ?? existingArtwork.status;
  const artwork = await prisma.artwork.update({
    where: { id: artworkId },
    data: {
      title: body.title === undefined ? undefined : body.title?.trim() || null,
      description: body.description === undefined ? undefined : body.description?.trim() || null,
      kind,
      source: body.source === undefined ? undefined : body.source?.trim() || null,
      status: body.status,
      schemaVersion,
      imageUrl,
      thumbnailUrl,
      metadata: metadata === undefined ? undefined : metadata ?? Prisma.JsonNull,
      analysis: analysis === undefined ? undefined : analysis ?? Prisma.JsonNull,
      publishedAt:
        body.status === undefined
          ? undefined
          : nextStatus === "PUBLISHED" && existingArtwork.publishedAt == null
            ? new Date()
            : nextStatus === "DRAFT"
              ? null
              : existingArtwork.publishedAt,
    },
  });

  return mapArtwork(artwork);
};
