-- CreateTable
CREATE TABLE "Artwork" (
    "id" TEXT NOT NULL,
    "userId" TEXT NOT NULL,
    "title" TEXT,
    "description" TEXT,
    "kind" TEXT NOT NULL DEFAULT 'painting',
    "source" TEXT NOT NULL DEFAULT 'paint-canvas',
    "status" TEXT NOT NULL DEFAULT 'DRAFT',
    "schemaVersion" INTEGER NOT NULL DEFAULT 1,
    "imageUrl" TEXT NOT NULL,
    "thumbnailUrl" TEXT,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "Artwork_pkey" PRIMARY KEY ("id")
);

-- AddForeignKey
ALTER TABLE "Artwork" ADD CONSTRAINT "Artwork_userId_fkey" FOREIGN KEY ("userId") REFERENCES "User"("id") ON DELETE CASCADE ON UPDATE CASCADE;
