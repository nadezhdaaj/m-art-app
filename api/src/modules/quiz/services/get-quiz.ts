import { prisma } from "@/lib/db";
import { toQuizDetail } from "../mapper";
import { getProfileOrFail, getPublishedQuizOrFail, resolveQuestionFlow } from "./shared";

export const getQuiz = async (userId: string, quizId: string) => {
  const profile = await getProfileOrFail(userId);
  const quiz = await getPublishedQuizOrFail(quizId);

  const progress = await prisma.quizAttempt.findFirst({
    where: {
      profileId: profile.id,
      quizId,
    },
    orderBy: { createdAt: "desc" },
  });

  const questionIds = quiz.questions.map((question) => question.id);
  const { questionOrderIds, orderChanged } = resolveQuestionFlow(questionIds, progress);

  if (progress && orderChanged) {
    await prisma.quizAttempt.update({
      where: { id: progress.id },
      data: {
        questionOrderIds,
      },
    });
  }

  const questionsById = new Map(quiz.questions.map((question) => [question.id, question]));
  const orderedQuestions = questionOrderIds.flatMap((questionId) => {
    const question = questionsById.get(questionId);
    return question ? [question] : [];
  });

  return toQuizDetail(quiz, orderedQuestions, progress, questionOrderIds);
};
