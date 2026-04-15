import { prisma } from "@/lib/db";
import { ERROR_CODES } from "@/lib/http";
import { status } from "elysia";
import { toQuizCompletionResult } from "../model";
import { getCompletionRewards, getProfileOrFail } from "./shared";

export const getQuizResult = async (userId: string, quizId: string, attemptId: string) => {
  const profile = await getProfileOrFail(userId);

  const attempt = await prisma.quizAttempt.findFirst({
    where: {
      id: attemptId,
      profileId: profile.id,
      quizId,
      status: "COMPLETED",
    },
  });

  if (!attempt || !attempt.finishedAt) {
    throw status(404, {
      code: ERROR_CODES.QUIZ_NOT_FOUND,
      message: "Completed quiz result was not found.",
    });
  }

  return toQuizCompletionResult(attempt, await getCompletionRewards(profile.id, attempt.id));
};
