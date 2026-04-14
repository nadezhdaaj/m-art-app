import { t, type UnwrapSchema } from "elysia";
import { progressModels } from "../progress/model";

const QuizProgress = t.Object({
  currentQuestionIndex: t.Integer(),
  correctAnswers: t.Integer(),
  questionOrderIds: t.Array(t.String()),
  answeredQuestionIds: t.Array(t.String()),
  completedAt: t.Nullable(t.String({ format: "date-time" })),
});
export type QuizProgress = UnwrapSchema<typeof QuizProgress>;

const QuizBase = t.Object({
  id: t.String(),
  slug: t.String(),
  title: t.String(),
  description: t.Nullable(t.String()),
  previewImageUrl: t.Nullable(t.String()),
  type: t.Union([t.Literal("MULTIPLE_CHOICE"), t.Literal("YES_NO")]),
  museumImageUrl: t.Nullable(t.String()),
  totalQuestions: t.Integer(),
});
export type QuizBase = UnwrapSchema<typeof QuizBase>;

const QuizSummary = t.Composite([
  QuizBase,
  t.Object({
    progress: t.Nullable(QuizProgress),
  }),
]);
export type QuizSummary = UnwrapSchema<typeof QuizSummary>;

const QuizDetail = t.Composite([
  QuizBase,
  t.Object({
    questions: t.Array(
      t.Object({
        id: t.String(),
        text: t.String(),
        fact: t.String(),
        imageUrl: t.Nullable(t.String()),
        answers: t.Array(
          t.Object({
            id: t.String(),
            orderIndex: t.Integer(),
            text: t.String(),
          }),
        ),
      }),
    ),
    progress: t.Nullable(QuizProgress),
  }),
]);
export type QuizDetail = UnwrapSchema<typeof QuizDetail>;

const QuizAnswerSubmit = t.Object({
  answerId: t.String(),
});
export type QuizAnswerSubmit = UnwrapSchema<typeof QuizAnswerSubmit>;

const QuizAnswerResult = t.Object({
  isCorrect: t.Boolean(),
  correctAnswerId: t.String(),
  fact: t.String(),
  nextQuestionIndex: t.Integer(),
  answeredQuestionIds: t.Array(t.String()),
  progress: QuizProgress,
  completionResult: t.Nullable(
    t.Object({
      attemptId: t.String(),
      quizId: t.String(),
      score: t.Integer(),
      totalQuestions: t.Integer(),
      correctAnswers: t.Integer(),
      wrongAnswers: t.Integer(),
      accuracyPercent: t.Integer(),
      completedAt: t.String({ format: "date-time" }),
      rewards: progressModels.RewardSummary,
    }),
  ),
});
export type QuizAnswerResult = UnwrapSchema<typeof QuizAnswerResult>;

const QuizCompletionResult = t.Object({
  attemptId: t.String(),
  quizId: t.String(),
  score: t.Integer(),
  totalQuestions: t.Integer(),
  correctAnswers: t.Integer(),
  wrongAnswers: t.Integer(),
  accuracyPercent: t.Integer(),
  completedAt: t.String({ format: "date-time" }),
  rewards: progressModels.RewardSummary,
});
export type QuizCompletionResult = UnwrapSchema<typeof QuizCompletionResult>;

export const QuizModels = {
  QuizProgress,
  QuizBase,
  QuizSummary,
  QuizDetail,
  QuizAnswerSubmit,
  QuizAnswerResult,
  QuizCompletionResult,
  QuizList: t.Array(QuizSummary),
} as const;
