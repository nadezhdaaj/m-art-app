import { status } from "elysia";
import { type ArtworkStatus, Prisma } from "@/generated/prisma/client";
import { prisma } from "@/lib/db";
import { ERROR_CODES } from "@/lib/http";
import { getProfile } from "@/modules/profile/services";
import { applyEvent } from "@/modules/progress/services/apply-event";
import { mapArtwork } from "../mapper";
import type { CreateArtworkBody } from "../model";
import { parseOptionalInteger, parseOptionalJson } from "./shared";
import { uploadArtworkMedia } from "./upload-artwork-media";

const ARTWORK_CREATION_XP = Number(process.env.ARTWORK_CREATION_XP || 0);

export const createArtwork = async (userId: string, body: CreateArtworkBody) => {
  if (!body.image) {
    throw status(400, {
      code: ERROR_CODES.ARTWORK_CREATE_EMPTY,
      message: "Artwork image is required.",
    });
  }

  const profile = await getProfile(userId);
  const artworkId = crypto.randomUUID();
  const metadata = parseOptionalJson("metadataJson", body.metadataJson);
  const analysis = parseOptionalJson("analysisJson", body.analysisJson);
  const schemaVersion = parseOptionalInteger("schemaVersion", body.schemaVersion) ?? 1;
  const statusValue = (body.status ?? "DRAFT") as ArtworkStatus;
  const kind = body.kind.trim();

  if (!kind) {
    throw status(400, {
      code: ERROR_CODES.VALIDATION_FAILED,
      message: 'Field "kind" must not be empty.',
    });
  }

  const [imageUrl, thumbnailUrl] = await Promise.all([
    uploadArtworkMedia({
      userId,
      artworkId,
      file: body.image,
      fieldName: "image",
    }),
    body.thumbnail
      ? uploadArtworkMedia({
          userId,
          artworkId,
          file: body.thumbnail,
          fieldName: "thumbnail",
        })
      : Promise.resolve<string | null>(null),
  ]);

  const artwork = await prisma.$transaction(async (tx) => {
    const data: Prisma.ArtworkCreateInput = {
      id: artworkId,
      title: body.title?.trim() || null,
      description: body.description?.trim() || null,
      kind,
      source: body.source?.trim() || null,
      status: statusValue,
      schemaVersion,
      imageUrl,
      thumbnailUrl,
      publishedAt: statusValue === "PUBLISHED" ? new Date() : null,
      profile: {
        connect: {
          id: profile.id,
        },
      },
    };

    if (metadata !== undefined) {
      data.metadata = metadata;
    }

    if (analysis !== undefined) {
      data.analysis = analysis;
    }

    const createdArtwork = await tx.artwork.create({
      data,
    });

    if (ARTWORK_CREATION_XP > 0) {
      await applyEvent({
        tx,
        profileId: profile.id,
        sourceType: "ARTWORK_CREATED",
        sourceId: createdArtwork.id,
        eventType: "ARTWORK_CREATED",
        xpDelta: ARTWORK_CREATION_XP,
        idempotencyKey: `artwork-created:${createdArtwork.id}`,
        payload: {
          kind: createdArtwork.kind,
          source: createdArtwork.source,
        },
      });
    }

    return createdArtwork;
  });

  return mapArtwork(artwork);
};
