const QUIZ_COMPLETION_XP = 50;
const QUIZ_CORRECT_ANSWER_XP = 10;
const QUIZ_PERFECT_SCORE_BONUS_XP = 25;
const QUIZ_FIRST_COMPLETION_BONUS_XP = 20;

export const calculateQuizCompletionXp = (input: {
  correctAnswers: number;
  totalQuestions: number;
  isFirstCompletion: boolean;
}) => {
  const perfectBonusXp =
    input.totalQuestions > 0 && input.correctAnswers === input.totalQuestions
      ? QUIZ_PERFECT_SCORE_BONUS_XP
      : 0;

  const firstCompletionBonusXp = input.isFirstCompletion ? QUIZ_FIRST_COMPLETION_BONUS_XP : 0;

  const gainedXp =
    QUIZ_COMPLETION_XP +
    input.correctAnswers * QUIZ_CORRECT_ANSWER_XP +
    perfectBonusXp +
    firstCompletionBonusXp;

  return {
    gainedXp,
    breakdown: {
      completionXp: QUIZ_COMPLETION_XP,
      correctAnswersXp: input.correctAnswers * QUIZ_CORRECT_ANSWER_XP,
      perfectBonusXp,
      firstCompletionBonusXp,
    },
  };
};
