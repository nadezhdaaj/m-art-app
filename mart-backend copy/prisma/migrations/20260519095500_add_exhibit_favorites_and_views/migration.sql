-- CreateTable
CREATE TABLE "ExhibitFavorite" (
    "id" TEXT NOT NULL,
    "userId" TEXT NOT NULL,
    "exhibitId" TEXT NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "ExhibitFavorite_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "ExhibitView" (
    "id" TEXT NOT NULL,
    "userId" TEXT NOT NULL,
    "exhibitId" TEXT NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "ExhibitView_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE UNIQUE INDEX "ExhibitFavorite_userId_exhibitId_key" ON "ExhibitFavorite"("userId", "exhibitId");

-- CreateIndex
CREATE UNIQUE INDEX "ExhibitView_userId_exhibitId_key" ON "ExhibitView"("userId", "exhibitId");

-- AddForeignKey
ALTER TABLE "ExhibitFavorite" ADD CONSTRAINT "ExhibitFavorite_userId_fkey" FOREIGN KEY ("userId") REFERENCES "User"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "ExhibitView" ADD CONSTRAINT "ExhibitView_userId_fkey" FOREIGN KEY ("userId") REFERENCES "User"("id") ON DELETE CASCADE ON UPDATE CASCADE;
