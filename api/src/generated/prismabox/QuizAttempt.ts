import { t } from "elysia";

import { __transformDate__ } from "./__transformDate__";

import { __nullable__ } from "./__nullable__";

export const QuizAttemptPlain = t.Object(
  {
    id: t.String(),
    profileId: t.String(),
    quizId: t.String(),
    status: t.Union(
      [
        t.Literal("IN_PROGRESS"),
        t.Literal("COMPLETED"),
        t.Literal("ABANDONED"),
      ],
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
);

export const QuizAttemptRelations = t.Object(
  {
    profile: t.Object(
      {
        id: t.String(),
        userId: t.String(),
        displayName: __nullable__(t.String()),
        avatarUrl: __nullable__(t.String()),
        xp: t.Integer(),
        createdAt: t.Date(),
        updatedAt: t.Date(),
      },
      { additionalProperties: false },
    ),
    quiz: t.Object(
      {
        id: t.String(),
        slug: t.String(),
        title: t.String(),
        description: __nullable__(t.String()),
        previewImageUrl: __nullable__(t.String()),
        type: t.Union([t.Literal("MULTIPLE_CHOICE"), t.Literal("YES_NO")], {
          additionalProperties: false,
        }),
        status: t.Union(
          [t.Literal("DRAFT"), t.Literal("PUBLISHED"), t.Literal("ARCHIVED")],
          { additionalProperties: false },
        ),
        createdAt: t.Date(),
        updatedAt: t.Date(),
      },
      { additionalProperties: false },
    ),
    answers: t.Array(
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
      { additionalProperties: false },
    ),
  },
  { additionalProperties: false },
);

export const QuizAttemptPlainInputCreate = t.Object(
  {
    status: t.Optional(
      t.Union(
        [
          t.Literal("IN_PROGRESS"),
          t.Literal("COMPLETED"),
          t.Literal("ABANDONED"),
        ],
        { additionalProperties: false },
      ),
    ),
    currentQuestionIndex: t.Optional(t.Integer()),
    totalQuestions: t.Integer(),
    correctAnswers: t.Optional(t.Integer()),
    wrongAnswers: t.Optional(t.Integer()),
    score: t.Optional(t.Integer()),
    accuracyPercent: t.Optional(t.Integer()),
    questionOrderIds: t.Optional(__nullable__(t.Any())),
    answeredQuestionIds: t.Optional(__nullable__(t.Any())),
    startedAt: t.Optional(t.Date()),
    finishedAt: t.Optional(__nullable__(t.Date())),
  },
  { additionalProperties: false },
);

export const QuizAttemptPlainInputUpdate = t.Object(
  {
    status: t.Optional(
      t.Union(
        [
          t.Literal("IN_PROGRESS"),
          t.Literal("COMPLETED"),
          t.Literal("ABANDONED"),
        ],
        { additionalProperties: false },
      ),
    ),
    currentQuestionIndex: t.Optional(t.Integer()),
    totalQuestions: t.Optional(t.Integer()),
    correctAnswers: t.Optional(t.Integer()),
    wrongAnswers: t.Optional(t.Integer()),
    score: t.Optional(t.Integer()),
    accuracyPercent: t.Optional(t.Integer()),
    questionOrderIds: t.Optional(__nullable__(t.Any())),
    answeredQuestionIds: t.Optional(__nullable__(t.Any())),
    startedAt: t.Optional(t.Date()),
    finishedAt: t.Optional(__nullable__(t.Date())),
  },
  { additionalProperties: false },
);

export const QuizAttemptRelationsInputCreate = t.Object(
  {
    profile: t.Object(
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
    quiz: t.Object(
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
    answers: t.Optional(
      t.Object(
        {
          connect: t.Array(
            t.Object(
              {
                id: t.String({ additionalProperties: false }),
              },
              { additionalProperties: false },
            ),
            { additionalProperties: false },
          ),
        },
        { additionalProperties: false },
      ),
    ),
  },
  { additionalProperties: false },
);

export const QuizAttemptRelationsInputUpdate = t.Partial(
  t.Object(
    {
      profile: t.Object(
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
      quiz: t.Object(
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
      answers: t.Partial(
        t.Object(
          {
            connect: t.Array(
              t.Object(
                {
                  id: t.String({ additionalProperties: false }),
                },
                { additionalProperties: false },
              ),
              { additionalProperties: false },
            ),
            disconnect: t.Array(
              t.Object(
                {
                  id: t.String({ additionalProperties: false }),
                },
                { additionalProperties: false },
              ),
              { additionalProperties: false },
            ),
          },
          { additionalProperties: false },
        ),
      ),
    },
    { additionalProperties: false },
  ),
);

export const QuizAttemptWhere = t.Partial(
  t.Recursive(
    (Self) =>
      t.Object(
        {
          AND: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          NOT: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          OR: t.Array(Self, { additionalProperties: false }),
          id: t.String(),
          profileId: t.String(),
          quizId: t.String(),
          status: t.Union(
            [
              t.Literal("IN_PROGRESS"),
              t.Literal("COMPLETED"),
              t.Literal("ABANDONED"),
            ],
            { additionalProperties: false },
          ),
          currentQuestionIndex: t.Integer(),
          totalQuestions: t.Integer(),
          correctAnswers: t.Integer(),
          wrongAnswers: t.Integer(),
          score: t.Integer(),
          accuracyPercent: t.Integer(),
          questionOrderIds: t.Any(),
          answeredQuestionIds: t.Any(),
          startedAt: t.Date(),
          finishedAt: t.Date(),
          createdAt: t.Date(),
          updatedAt: t.Date(),
        },
        { additionalProperties: false },
      ),
    { $id: "QuizAttempt" },
  ),
);

export const QuizAttemptWhereUnique = t.Recursive(
  (Self) =>
    t.Intersect(
      [
        t.Partial(
          t.Object({ id: t.String() }, { additionalProperties: false }),
          { additionalProperties: false },
        ),
        t.Union([t.Object({ id: t.String() })], {
          additionalProperties: false,
        }),
        t.Partial(
          t.Object({
            AND: t.Union([
              Self,
              t.Array(Self, { additionalProperties: false }),
            ]),
            NOT: t.Union([
              Self,
              t.Array(Self, { additionalProperties: false }),
            ]),
            OR: t.Array(Self, { additionalProperties: false }),
          }),
          { additionalProperties: false },
        ),
        t.Partial(
          t.Object(
            {
              id: t.String(),
              profileId: t.String(),
              quizId: t.String(),
              status: t.Union(
                [
                  t.Literal("IN_PROGRESS"),
                  t.Literal("COMPLETED"),
                  t.Literal("ABANDONED"),
                ],
                { additionalProperties: false },
              ),
              currentQuestionIndex: t.Integer(),
              totalQuestions: t.Integer(),
              correctAnswers: t.Integer(),
              wrongAnswers: t.Integer(),
              score: t.Integer(),
              accuracyPercent: t.Integer(),
              questionOrderIds: t.Any(),
              answeredQuestionIds: t.Any(),
              startedAt: t.Date(),
              finishedAt: t.Date(),
              createdAt: t.Date(),
              updatedAt: t.Date(),
            },
            { additionalProperties: false },
          ),
        ),
      ],
      { additionalProperties: false },
    ),
  { $id: "QuizAttempt" },
);

export const QuizAttemptSelect = t.Partial(
  t.Object(
    {
      id: t.Boolean(),
      profileId: t.Boolean(),
      quizId: t.Boolean(),
      status: t.Boolean(),
      currentQuestionIndex: t.Boolean(),
      totalQuestions: t.Boolean(),
      correctAnswers: t.Boolean(),
      wrongAnswers: t.Boolean(),
      score: t.Boolean(),
      accuracyPercent: t.Boolean(),
      questionOrderIds: t.Boolean(),
      answeredQuestionIds: t.Boolean(),
      startedAt: t.Boolean(),
      finishedAt: t.Boolean(),
      createdAt: t.Boolean(),
      updatedAt: t.Boolean(),
      profile: t.Boolean(),
      quiz: t.Boolean(),
      answers: t.Boolean(),
      _count: t.Boolean(),
    },
    { additionalProperties: false },
  ),
);

export const QuizAttemptInclude = t.Partial(
  t.Object(
    {
      status: t.Boolean(),
      profile: t.Boolean(),
      quiz: t.Boolean(),
      answers: t.Boolean(),
      _count: t.Boolean(),
    },
    { additionalProperties: false },
  ),
);

export const QuizAttemptOrderBy = t.Partial(
  t.Object(
    {
      id: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      profileId: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      quizId: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      currentQuestionIndex: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      totalQuestions: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      correctAnswers: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      wrongAnswers: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      score: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      accuracyPercent: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      questionOrderIds: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      answeredQuestionIds: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      startedAt: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      finishedAt: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      createdAt: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      updatedAt: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
    },
    { additionalProperties: false },
  ),
);

export const QuizAttempt = t.Composite(
  [QuizAttemptPlain, QuizAttemptRelations],
  { additionalProperties: false },
);

export const QuizAttemptInputCreate = t.Composite(
  [QuizAttemptPlainInputCreate, QuizAttemptRelationsInputCreate],
  { additionalProperties: false },
);

export const QuizAttemptInputUpdate = t.Composite(
  [QuizAttemptPlainInputUpdate, QuizAttemptRelationsInputUpdate],
  { additionalProperties: false },
);
