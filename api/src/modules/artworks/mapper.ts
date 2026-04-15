import type { Artwork } from "@/generated/prisma/client";
import { s3Storage } from "@/lib/s3";

export const mapArtwork = (artwork: Artwork) => ({
  id: artwork.id,
  title: artwork.title,
  description: artwork.description,
  kind: artwork.kind,
  source: artwork.source,
  status: artwork.status,
  schemaVersion: artwork.schemaVersion,
  imageUrl: s3Storage.resolvePublicUrl(artwork.imageUrl) ?? "",
  thumbnailUrl: s3Storage.resolvePublicUrl(artwork.thumbnailUrl),
  metadata: artwork.metadata,
  analysis: artwork.analysis,
  publishedAt: artwork.publishedAt?.toISOString() ?? null,
  createdAt: artwork.createdAt.toISOString(),
  updatedAt: artwork.updatedAt.toISOString(),
});
