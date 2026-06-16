/*
  Warnings:

  - You are about to drop the column `isCorrect` on the `QuizAnswer` table. All the data in the column will be lost.
  - A unique constraint covering the columns `[question]` on the table `QuizQuestion` will be added. If there are existing duplicate values, this will fail.

*/
-- AlterTable
ALTER TABLE "QuizAnswer" DROP COLUMN "isCorrect";

-- CreateIndex
CREATE UNIQUE INDEX "QuizQuestion_question_key" ON "QuizQuestion"("question");
