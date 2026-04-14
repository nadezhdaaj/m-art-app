import { s3Storage } from "@/lib/s3";
import type { QuizDetail, QuizProgress, QuizSummary } from "./model";
import { toStringArray } from "./utils";

type QuizProgressSource = {
  currentQuestionIndex: number;
  correctAnswers: number;
  questionOrderIds: unknown;
  answeredQuestionIds: unknown;
  finishedAt: Date | null;
};

type QuizListItemSource = {
  id: string;
  slug: string;
  title: string;
  description: string | null;
  previewImageUrl: string | null;
  type: "MULTIPLE_CHOICE" | "YES_NO";
  imagePool: Array<{ imageUrl: string | null }>;
};

type QuizQuestionSource = {
  id: string;
  text: string;
  fact: string;
  imageUrl: string | null;
  answers: Array<{
    id: string;
    orderIndex: number;
    text: string;
  }>;
};

export const toQuizProgress = (
  progress: QuizProgressSource | null,
  questionOrderIds: string[],
): QuizProgress | null =>
  progress
    ? {
        currentQuestionIndex: progress.currentQuestionIndex,
        correctAnswers: progress.correctAnswers,
        questionOrderIds,
        answeredQuestionIds: toStringArray(progress.answeredQuestionIds),
        completedAt: progress.finishedAt?.toISOString() ?? null,
      }
    : null;

export const resolveQuizMuseumImageUrl = (imagePool: Array<{ imageUrl: string | null }>) =>
  s3Storage.resolvePublicUrl(
    imagePool[Math.floor(Math.random() * imagePool.length)]?.imageUrl ?? null,
  );

export const toQuizSummary = (
  quiz: QuizListItemSource,
  totalQuestions: number,
  progress: QuizProgressSource | null,
  questionOrderIds: string[],
): QuizSummary => ({
  id: quiz.id,
  slug: quiz.slug,
  title: quiz.title,
  description: quiz.description,
  previewImageUrl: s3Storage.resolvePublicUrl(quiz.previewImageUrl),
  type: quiz.type,
  museumImageUrl: resolveQuizMuseumImageUrl(quiz.imagePool),
  totalQuestions,
  progress: toQuizProgress(progress, questionOrderIds),
});

export const toQuizDetail = (
  quiz: QuizListItemSource,
  questions: QuizQuestionSource[],
  progress: QuizProgressSource | null,
  questionOrderIds: string[],
): QuizDetail => ({
  id: quiz.id,
  slug: quiz.slug,
  title: quiz.title,
  description: quiz.description,
  previewImageUrl: s3Storage.resolvePublicUrl(quiz.previewImageUrl),
  museumImageUrl: resolveQuizMuseumImageUrl(quiz.imagePool),
  type: quiz.type,
  totalQuestions: questions.length,
  questions: questions.map((question) => ({
    id: question.id,
    text: question.text,
    fact: question.fact,
    imageUrl: s3Storage.resolvePublicUrl(question.imageUrl),
    answers: question.answers.map((answer) => ({
      id: answer.id,
      orderIndex: answer.orderIndex,
      text: answer.text,
    })),
  })),
  progress: toQuizProgress(progress, questionOrderIds),
});
