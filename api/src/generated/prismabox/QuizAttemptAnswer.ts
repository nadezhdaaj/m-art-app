import { t } from "elysia";
import { __nullable__ } from "./__nullable__";
import { __transformDate__ } from "./__transformDate__";

export const QuizAttemptAnswerPlain = t.Object(
  {
    id: t.String(),
    attemptId: t.String(),
    questionId: t.String(),
    selectedAnswerId: t.String(),
    isCorrect: t.Boolean(),
    answeredAt: t.Date(),
  },
  { additionalProperties: false },
);

export const QuizAttemptAnswerRelations = t.Object(
  {
    attempt: t.Object(
      {
        id: t.String(),
        profileId: t.String(),
        quizId: t.String(),
        status: t.Union(
          [t.Literal("IN_PROGRESS"), t.Literal("COMPLETED"), t.Literal("ABANDONED")],
          { additionalProperties: false },
        ),
        currentQuestionIndex: t.Integer(),
        totalQuestions: t.Integer(),
        correctAnswers: t.Integer(),
        wrongAnswers: t.Integer(),
        score: t.Integer(),
        accuracyPercent: t.Integer(),
        questionOrderIds: __nullable__(t.Any()),
        answeredQuestionIds: __nullable__(t.Any()),
        startedAt: t.Date(),
        finishedAt: __nullable__(t.Date()),
        createdAt: t.Date(),
        updatedAt: t.Date(),
      },
      { additionalProperties: false },
    ),
  },
  { additionalProperties: false },
);

export const QuizAttemptAnswerPlainInputCreate = t.Object(
  { isCorrect: t.Boolean(), answeredAt: t.Optional(t.Date()) },
  { additionalProperties: false },
);

export const QuizAttemptAnswerPlainInputUpdate = t.Object(
  { isCorrect: t.Optional(t.Boolean()), answeredAt: t.Optional(t.Date()) },
  { additionalProperties: false },
);

export const QuizAttemptAnswerRelationsInputCreate = t.Object(
  {
    attempt: t.Object(
      {
        connect: t.Object(
          {
            id: t.String({ additionalProperties: false }),
          },
          { additionalProperties: false },
        ),
      },
      { additionalProperties: false },
    ),
  },
  { additionalProperties: false },
);

export const QuizAttemptAnswerRelationsInputUpdate = t.Partial(
  t.Object(
    {
      attempt: t.Object(
        {
          connect: t.Object(
            {
              id: t.String({ additionalProperties: false }),
            },
            { additionalProperties: false },
          ),
        },
        { additionalProperties: false },
      ),
    },
    { additionalProperties: false },
  ),
);

export const QuizAttemptAnswerWhere = t.Partial(
  t.Recursive(
    (Self) =>
      t.Object(
        {
          AND: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          NOT: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          OR: t.Array(Self, { additionalProperties: false }),
          id: t.String(),
          attemptId: t.String(),
          questionId: t.String(),
          selectedAnswerId: t.String(),
          isCorrect: t.Boolean(),
          answeredAt: t.Date(),
        },
        { additionalProperties: false },
      ),
    { $id: "QuizAttemptAnswer" },
  ),
);

export const QuizAttemptAnswerWhereUnique = t.Recursive(
  (Self) =>
    t.Intersect(
      [
        t.Partial(
          t.Object(
            {
              id: t.String(),
              attemptId_questionId: t.Object(
                { attemptId: t.String(), questionId: t.String() },
                { additionalProperties: false },
              ),
            },
            { additionalProperties: false },
          ),
          { additionalProperties: false },
        ),
        t.Union(
          [
            t.Object({ id: t.String() }),
            t.Object({
              attemptId_questionId: t.Object(
                { attemptId: t.String(), questionId: t.String() },
                { additionalProperties: false },
              ),
            }),
          ],
          { additionalProperties: false },
        ),
        t.Partial(
          t.Object({
            AND: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
            NOT: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
            OR: t.Array(Self, { additionalProperties: false }),
          }),
          { additionalProperties: false },
        ),
        t.Partial(
          t.Object(
            {
              id: t.String(),
              attemptId: t.String(),
              questionId: t.String(),
              selectedAnswerId: t.String(),
              isCorrect: t.Boolean(),
              answeredAt: t.Date(),
            },
            { additionalProperties: false },
          ),
        ),
      ],
      { additionalProperties: false },
    ),
  { $id: "QuizAttemptAnswer" },
);

export const QuizAttemptAnswerSelect = t.Partial(
  t.Object(
    {
      id: t.Boolean(),
      attemptId: t.Boolean(),
      questionId: t.Boolean(),
      selectedAnswerId: t.Boolean(),
      isCorrect: t.Boolean(),
      answeredAt: t.Boolean(),
      attempt: t.Boolean(),
      _count: t.Boolean(),
    },
    { additionalProperties: false },
  ),
);

export const QuizAttemptAnswerInclude = t.Partial(
  t.Object({ attempt: t.Boolean(), _count: t.Boolean() }, { additionalProperties: false }),
);

export const QuizAttemptAnswerOrderBy = t.Partial(
  t.Object(
    {
      id: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      attemptId: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      questionId: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      selectedAnswerId: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      isCorrect: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      answeredAt: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
    },
    { additionalProperties: false },
  ),
);

export const QuizAttemptAnswer = t.Composite([QuizAttemptAnswerPlain, QuizAttemptAnswerRelations], {
  additionalProperties: false,
});

export const QuizAttemptAnswerInputCreate = t.Composite(
  [QuizAttemptAnswerPlainInputCreate, QuizAttemptAnswerRelationsInputCreate],
  { additionalProperties: false },
);

export const QuizAttemptAnswerInputUpdate = t.Composite(
  [QuizAttemptAnswerPlainInputUpdate, QuizAttemptAnswerRelationsInputUpdate],
  { additionalProperties: false },
);
