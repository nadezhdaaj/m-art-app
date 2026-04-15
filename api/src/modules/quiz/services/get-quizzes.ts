import { prisma } from "@/lib/db";
import { toQuizSummary } from "../mapper";
import { getProfileOrFail, resolveQuestionFlow } from "./shared";

export const getQuizzes = async (userId: string) => {
  const profile = await getProfileOrFail(userId);

  const [quizzes, attempts] = await Promise.all([
    prisma.quiz.findMany({
      where: {
        status: "PUBLISHED",
      },
      include: {
        imagePool: {
          orderBy: { sortOrder: "asc" },
        },
        questions: {
          select: { id: true },
        },
      },
      orderBy: { createdAt: "asc" },
    }),
    prisma.quizAttempt.findMany({
      where: { profileId: profile.id },
      orderBy: { createdAt: "desc" },
    }),
  ]);

  const progressByQuizId = new Map<string, (typeof attempts)[number]>();
  for (const attempt of attempts) {
    if (!progressByQuizId.has(attempt.quizId)) {
      progressByQuizId.set(attempt.quizId, attempt);
    }
  }

  return quizzes.map((quiz) => {
    const progress = progressByQuizId.get(quiz.id) ?? null;
    const questionIds = quiz.questions.map((question) => question.id);
    const { questionOrderIds } = resolveQuestionFlow(questionIds, progress);

    return toQuizSummary(quiz, questionIds.length, progress, questionOrderIds);
  });
};
