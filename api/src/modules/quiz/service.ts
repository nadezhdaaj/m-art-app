import { status } from "elysia";
import { prisma } from "@/lib/db";
import { ERROR_CODES } from "@/lib/http";
import { ProfileService } from "../profile/service";
import { getLevelByXp } from "../progress/level";
import type { RewardSummary } from "../progress/model";
import { RewardService } from "../rewards/reward.service";
import { toQuizDetail, toQuizProgress, toQuizSummary } from "./mapper";
import type { QuizAnswerResult } from "./model";
import { toQuizCompletionResult } from "./result.mapper";
import { shuffle, toStringArray } from "./utils";

const publishedQuizInclude = {
  questions: {
    include: {
      answers: {
        orderBy: { orderIndex: "asc" as const },
      },
    },
  },
  imagePool: {
    orderBy: { sortOrder: "asc" as const },
  },
} as const;

type QuizProgressRecord = {
  currentQuestionIndex: number;
  correctAnswers: number;
  questionOrderIds: unknown;
  answeredQuestionIds: unknown;
  finishedAt: Date | null;
};

const getProfileOrFail = (userId: string) => ProfileService.getProfile(userId);

const getPublishedQuizOrFail = async (
  quizId: string,
  client: Pick<typeof prisma, "quiz"> = prisma,
) => {
  const quiz = await client.quiz.findFirst({
    where: {
      id: quizId,
      status: "PUBLISHED",
    },
    include: publishedQuizInclude,
  });

  if (!quiz) {
    throw status(404, {
      code: ERROR_CODES.QUIZ_NOT_FOUND,
      message: "Quiz was not found.",
    });
  }

  return quiz;
};

const resolveQuestionFlow = (questionIds: string[], progress: QuizProgressRecord | null) => {
  const savedOrder = toStringArray(progress?.questionOrderIds).filter((id) =>
    questionIds.includes(id),
  );
  const missingQuestionIds = questionIds.filter((id) => !savedOrder.includes(id));
  const questionOrderIds =
    savedOrder.length === questionIds.length
      ? savedOrder
      : [...savedOrder, ...shuffle(missingQuestionIds)];

  return {
    questionOrderIds,
    answeredQuestionIds: toStringArray(progress?.answeredQuestionIds),
    orderChanged: savedOrder.join(",") !== questionOrderIds.join(","),
  };
};

const getQuestionOrFail = <TQuestion extends { id: string }>(
  questions: TQuestion[],
  questionId: string,
) => {
  const question = questions.find((entry) => entry.id === questionId);

  if (!question) {
    throw status(404, {
      code: ERROR_CODES.QUIZ_QUESTION_NOT_FOUND,
      message: "Question was not found in the selected quiz.",
    });
  }

  return question;
};

const getAnswerPairOrFail = <
  TAnswer extends {
    id: string;
    isCorrect: boolean;
  },
>(
  answers: TAnswer[],
  answerId: string,
) => {
  const selectedAnswer = answers.find((answer) => answer.id === answerId);
  const correctAnswer = answers.find((answer) => answer.isCorrect);

  if (!selectedAnswer || !correctAnswer) {
    throw status(404, {
      code: ERROR_CODES.QUIZ_ANSWER_NOT_FOUND,
      message: "Answer was not found for the selected question.",
    });
  }

  return { selectedAnswer, correctAnswer };
};

const getCompletionRewards = async (
  profileId: string,
  attemptId: string,
): Promise<RewardSummary> => {
  const attemptEvents = await prisma.progressEvent.findMany({
    where: {
      profileId,
      sourceType: "QUIZ_ATTEMPT",
      sourceId: attemptId,
    },
    orderBy: { createdAt: "asc" },
  });

  const profile = await prisma.profile.findUniqueOrThrow({
    where: { id: profileId },
  });

  const quizCompletedEvent = attemptEvents.find((event) => event.eventType === "QUIZ_COMPLETED");
  const achievementEvents = attemptEvents.filter(
    (event) => event.eventType === "ACHIEVEMENT_UNLOCKED",
  );
  const gainedXp = attemptEvents.reduce((total, event) => total + event.xpDelta, 0);

  const unlockedAchievements = achievementEvents.flatMap((event) => {
    if (!event.payload || typeof event.payload !== "object" || Array.isArray(event.payload)) {
      return [];
    }

    const payload = event.payload as Record<string, unknown>;
    const achievementId = typeof payload.achievementId === "string" ? payload.achievementId : null;
    const achievementCode =
      typeof payload.achievementCode === "string" ? payload.achievementCode : null;
    const achievementName =
      typeof payload.achievementName === "string" ? payload.achievementName : null;

    if (!achievementId || !achievementCode || !achievementName) {
      return [];
    }

    return [
      {
        id: achievementId,
        code: achievementCode,
        name: achievementName,
        description: null,
        iconUrl: null,
        category: "quiz",
        rarity: null,
        unlockedAt: event.createdAt.toISOString(),
        progress: 100,
      },
    ];
  });

  const previousLevel =
    quizCompletedEvent?.payload &&
    typeof quizCompletedEvent.payload === "object" &&
    !Array.isArray(quizCompletedEvent.payload) &&
    typeof (quizCompletedEvent.payload as Record<string, unknown>).previousLevel === "number"
      ? ((quizCompletedEvent.payload as Record<string, unknown>).previousLevel as number)
      : getLevelByXp(profile.xp);

  return {
    gainedXp,
    totalXp: profile.xp,
    previousLevel,
    currentLevel: getLevelByXp(profile.xp),
    leveledUp: getLevelByXp(profile.xp) > previousLevel,
    unlockedAchievements,
  };
};

export abstract class QuizService {
  static async getQuizzes(userId: string) {
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
  }

  static async getQuiz(userId: string, quizId: string) {
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
  }

  static async submitAnswer(
    userId: string,
    quizId: string,
    questionId: string,
    answerId: string,
  ): Promise<QuizAnswerResult> {
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
            await RewardService.grantQuizCompletionRewards({
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
  }

  static async getQuizResult(userId: string, quizId: string, attemptId: string) {
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
  }
}
