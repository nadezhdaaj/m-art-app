import type { RewardSummary } from "../progress/model";
import type { QuizCompletionResult } from "./model";

type QuizAttemptResultSource = {
  id: string;
  quizId: string;
  score: number;
  totalQuestions: number;
  correctAnswers: number;
  wrongAnswers: number;
  accuracyPercent: number;
  finishedAt: Date | null;
};

export const toQuizCompletionResult = (
  attempt: QuizAttemptResultSource,
  rewards: RewardSummary,
): QuizCompletionResult => ({
  attemptId: attempt.id,
  quizId: attempt.quizId,
  score: attempt.score,
  totalQuestions: attempt.totalQuestions,
  correctAnswers: attempt.correctAnswers,
  wrongAnswers: attempt.wrongAnswers,
  accuracyPercent: attempt.accuracyPercent,
  completedAt: attempt.finishedAt?.toISOString() ?? new Date().toISOString(),
  rewards,
});
