-- CreateEnum
CREATE TYPE "NewsPostStatus" AS ENUM ('DRAFT', 'PUBLISHED', 'ARCHIVED');

-- CreateEnum
CREATE TYPE "NewsPostSourceType" AS ENUM ('MANUAL', 'IMPORT');

-- CreateTable
CREATE TABLE "news_post" (
    "id" TEXT NOT NULL,
    "slug" TEXT NOT NULL,
    "title" TEXT NOT NULL,
    "excerpt" TEXT,
    "coverImageUrl" TEXT,
    "content" TEXT NOT NULL,
    "status" "NewsPostStatus" NOT NULL DEFAULT 'DRAFT',
    "sourceType" "NewsPostSourceType" NOT NULL DEFAULT 'MANUAL',
    "sourceUrl" TEXT,
    "sourceExternalId" TEXT,
    "publishedAt" TIMESTAMP(3),
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "news_post_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE UNIQUE INDEX "news_post_slug_key" ON "news_post"("slug");

-- CreateIndex
CREATE INDEX "news_post_status_publishedAt_idx" ON "news_post"("status", "publishedAt" DESC);

-- CreateIndex
CREATE INDEX "news_post_sourceType_idx" ON "news_post"("sourceType");
