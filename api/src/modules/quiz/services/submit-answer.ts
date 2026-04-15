import { status } from "elysia";
import { prisma } from "@/lib/db";
import { ERROR_CODES } from "@/lib/http";
import { toQuizProgress } from "../mapper";
import { toQuizCompletionResult, type QuizAnswerResult } from "../model";
import { shuffle, toStringArray } from "../utils";
import {
  getAnswerPairOrFail,
  getProfileOrFail,
  getPublishedQuizOrFail,
  getQuestionOrFail,
  resolveQuestionFlow,
} from "./shared";
import { grantQuizCompletionRewards } from "./grant-quiz-completion-rewards";

export const submitAnswer = async (
  userId: string,
  quizId: string,
  questionId: string,
  answerId: string,
): Promise<QuizAnswerResult> => {
  const profile = await getProfileOrFail(userId);

  return prisma.$transaction(async (tx) => {
    const quiz = await getPublishedQuizOrFail(quizId, tx);
    const question = getQuestionOrFail(quiz.questions, questionId);
    const { selectedAnswer, correctAnswer } = getAnswerPairOrFail(question.answers, answerId);

    const existingAttempt = await tx.quizAttempt.findFirst({
      where: {
        profileId: profile.id,
        quizId: quiz.id,
        status: "IN_PROGRESS",
      },
      orderBy: { createdAt: "desc" },
    });

    const progress =
      existingAttempt ??
      (await tx.quizAttempt.create({
        data: {
          profileId: profile.id,
          quizId: quiz.id,
          totalQuestions: quiz.questions.length,
          questionOrderIds: shuffle(quiz.questions.map((entry) => entry.id)),
        },
      }));

    const questionIds = quiz.questions.map((entry) => entry.id);
    const { questionOrderIds, answeredQuestionIds } = resolveQuestionFlow(questionIds, progress);

    if (!questionOrderIds.includes(question.id)) {
      throw status(409, {
        code: ERROR_CODES.QUIZ_QUESTION_ALREADY_ANSWERED,
        message: "Question flow is inconsistent for the selected quiz attempt.",
      });
    }

    if (progress.finishedAt) {
      throw status(409, {
        code: ERROR_CODES.QUIZ_QUESTION_ALREADY_ANSWERED,
        message: "This quiz attempt has already been completed.",
      });
    }

    if (answeredQuestionIds.includes(question.id)) {
      throw status(409, {
        code: ERROR_CODES.QUIZ_QUESTION_ALREADY_ANSWERED,
        message: "This question has already been answered.",
      });
    }

    const isCorrect = selectedAnswer.id === correctAnswer.id;
    const nextAnsweredQuestionIds = [...answeredQuestionIds, question.id];
    const nextQuestionIndex = nextAnsweredQuestionIds.length;
    const correctAnswers = progress.correctAnswers + Number(isCorrect);
    const wrongAnswers = nextAnsweredQuestionIds.length - correctAnswers;
    const accuracyPercent =
      questionIds.length > 0 ? Math.round((correctAnswers / questionIds.length) * 100) : 0;
    const isCompleted = nextQuestionIndex >= questionIds.length;

    await tx.quizAttemptAnswer.create({
      data: {
        attemptId: progress.id,
        questionId: question.id,
        selectedAnswerId: selectedAnswer.id,
        isCorrect,
      },
    });

    const updatedProgress = await tx.quizAttempt.update({
      where: { id: progress.id },
      data: {
        currentQuestionIndex: nextQuestionIndex,
        totalQuestions: questionIds.length,
        correctAnswers,
        wrongAnswers,
        score: correctAnswers,
        accuracyPercent,
        questionOrderIds,
        answeredQuestionIds: nextAnsweredQuestionIds,
        status: isCompleted ? "COMPLETED" : progress.status,
        finishedAt: isCompleted ? new Date() : null,
      },
    });

    const completionResult = isCompleted
      ? toQuizCompletionResult(
          updatedProgress,
          await grantQuizCompletionRewards({
            tx,
            profileId: profile.id,
            quizId: quiz.id,
            attemptId: updatedProgress.id,
            totalQuestions: questionIds.length,
            correctAnswers,
          }),
        )
      : null;

    return {
      isCorrect,
      correctAnswerId: correctAnswer.id,
      fact: question.fact,
      nextQuestionIndex: updatedProgress.currentQuestionIndex,
      answeredQuestionIds: toStringArray(updatedProgress.answeredQuestionIds),
      progress: toQuizProgress(updatedProgress, questionOrderIds)!,
      completionResult,
    };
  });
};
