CREATE TYPE "ArtworkStatus" AS ENUM ('DRAFT', 'PUBLISHED', 'ARCHIVED');

ALTER TYPE "ProgressEventType" ADD VALUE IF NOT EXISTS 'ARTWORK_CREATED';

CREATE TABLE "artwork" (
    "id" TEXT NOT NULL,
    "profileId" TEXT NOT NULL,
    "title" TEXT,
    "description" TEXT,
    "kind" TEXT NOT NULL,
    "source" TEXT,
    "status" "ArtworkStatus" NOT NULL DEFAULT 'DRAFT',
    "schemaVersion" INTEGER NOT NULL DEFAULT 1,
    "imageUrl" TEXT NOT NULL,
    "thumbnailUrl" TEXT,
    "metadata" JSONB,
    "analysis" JSONB,
    "publishedAt" TIMESTAMP(3),
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "artwork_pkey" PRIMARY KEY ("id")
);

CREATE INDEX "artwork_profileId_createdAt_idx" ON "artwork"("profileId", "createdAt" DESC);
CREATE INDEX "artwork_profileId_status_createdAt_idx" ON "artwork"("profileId", "status", "createdAt" DESC);
CREATE INDEX "artwork_kind_idx" ON "artwork"("kind");

ALTER TABLE "artwork" ADD CONSTRAINT "artwork_profileId_fkey"
FOREIGN KEY ("profileId") REFERENCES "profile"("id") ON DELETE CASCADE ON UPDATE CASCADE;
