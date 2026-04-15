import { status } from "elysia";
import { prisma } from "@/lib/db";
import { ERROR_CODES } from "@/lib/http";
import { getProfile } from "@/modules/profile/services";
import { getAchievementIdsFromEvents, mapAchievementSummary } from "@/modules/progress/achievement.mapper";
import { getLevelByXp } from "@/modules/progress/level";
import type { RewardSummary } from "@/modules/progress/model";
import { shuffle, toStringArray } from "../utils";

export const publishedQuizInclude = {
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

export type QuizProgressRecord = {
  currentQuestionIndex: number;
  correctAnswers: number;
  questionOrderIds: unknown;
  answeredQuestionIds: unknown;
  finishedAt: Date | null;
};

export const getProfileOrFail = (userId: string) => getProfile(userId);

export const getPublishedQuizOrFail = async (
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

export const resolveQuestionFlow = (questionIds: string[], progress: QuizProgressRecord | null) => {
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

export const getQuestionOrFail = <TQuestion extends { id: string }>(
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

export const getAnswerPairOrFail = <
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

export const getCompletionRewards = async (
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
  const unlockedAchievementIds = getAchievementIdsFromEvents(achievementEvents);
  const unlockedAchievements =
    unlockedAchievementIds.length === 0
      ? []
      : await prisma.userAchievement.findMany({
          where: {
            profileId,
            achievementId: { in: unlockedAchievementIds },
          },
          include: {
            achievement: true,
          },
          orderBy: { unlockedAt: "asc" },
        });

  const previousLevel =
    quizCompletedEvent?.payload &&
    typeof quizCompletedEvent.payload === "object" &&
    !Array.isArray(quizCompletedEvent.payload) &&
    typeof (quizCompletedEvent.payload as Record<string, unknown>).previousLevel === "number"
      ? ((quizCompletedEvent.payload as Record<string, unknown>).previousLevel as number)
      : getLevelByXp(profile.xp);

  const currentLevel = getLevelByXp(profile.xp);

  return {
    gainedXp,
    totalXp: profile.xp,
    previousLevel,
    currentLevel,
    leveledUp: currentLevel > previousLevel,
    unlockedAchievements: unlockedAchievements.map(mapAchievementSummary),
  };
};
