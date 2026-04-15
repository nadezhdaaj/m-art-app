import { t } from "elysia";
import { __nullable__ } from "./__nullable__";
import { __transformDate__ } from "./__transformDate__";

export const QuizAnswerPlain = t.Object(
  {
    id: t.String(),
    questionId: t.String(),
    orderIndex: t.Integer(),
    text: t.String(),
    isCorrect: t.Boolean(),
    createdAt: t.Date(),
    updatedAt: t.Date(),
  },
  { additionalProperties: false },
);

export const QuizAnswerRelations = t.Object(
  {
    question: t.Object(
      {
        id: t.String(),
        quizId: t.String(),
        text: t.String(),
        fact: t.String(),
        imageUrl: __nullable__(t.String()),
        createdAt: t.Date(),
        updatedAt: t.Date(),
      },
      { additionalProperties: false },
    ),
  },
  { additionalProperties: false },
);

export const QuizAnswerPlainInputCreate = t.Object(
  {
    orderIndex: t.Integer(),
    text: t.String(),
    isCorrect: t.Optional(t.Boolean()),
  },
  { additionalProperties: false },
);

export const QuizAnswerPlainInputUpdate = t.Object(
  {
    orderIndex: t.Optional(t.Integer()),
    text: t.Optional(t.String()),
    isCorrect: t.Optional(t.Boolean()),
  },
  { additionalProperties: false },
);

export const QuizAnswerRelationsInputCreate = t.Object(
  {
    question: t.Object(
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

export const QuizAnswerRelationsInputUpdate = t.Partial(
  t.Object(
    {
      question: t.Object(
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

export const QuizAnswerWhere = t.Partial(
  t.Recursive(
    (Self) =>
      t.Object(
        {
          AND: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          NOT: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          OR: t.Array(Self, { additionalProperties: false }),
          id: t.String(),
          questionId: t.String(),
          orderIndex: t.Integer(),
          text: t.String(),
          isCorrect: t.Boolean(),
          createdAt: t.Date(),
          updatedAt: t.Date(),
        },
        { additionalProperties: false },
      ),
    { $id: "QuizAnswer" },
  ),
);

export const QuizAnswerWhereUnique = t.Recursive(
  (Self) =>
    t.Intersect(
      [
        t.Partial(
          t.Object(
            {
              id: t.String(),
              questionId_orderIndex: t.Object(
                { questionId: t.String(), orderIndex: t.Integer() },
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
              questionId_orderIndex: t.Object(
                { questionId: t.String(), orderIndex: t.Integer() },
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
              questionId: t.String(),
              orderIndex: t.Integer(),
              text: t.String(),
              isCorrect: t.Boolean(),
              createdAt: t.Date(),
              updatedAt: t.Date(),
            },
            { additionalProperties: false },
          ),
        ),
      ],
      { additionalProperties: false },
    ),
  { $id: "QuizAnswer" },
);

export const QuizAnswerSelect = t.Partial(
  t.Object(
    {
      id: t.Boolean(),
      questionId: t.Boolean(),
      orderIndex: t.Boolean(),
      text: t.Boolean(),
      isCorrect: t.Boolean(),
      createdAt: t.Boolean(),
      updatedAt: t.Boolean(),
      question: t.Boolean(),
      _count: t.Boolean(),
    },
    { additionalProperties: false },
  ),
);

export const QuizAnswerInclude = t.Partial(
  t.Object({ question: t.Boolean(), _count: t.Boolean() }, { additionalProperties: false }),
);

export const QuizAnswerOrderBy = t.Partial(
  t.Object(
    {
      id: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      questionId: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      orderIndex: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      text: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      isCorrect: t.Union([t.Literal("asc"), t.Literal("desc")], {
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

export const QuizAnswer = t.Composite([QuizAnswerPlain, QuizAnswerRelations], {
  additionalProperties: false,
});

export const QuizAnswerInputCreate = t.Composite(
  [QuizAnswerPlainInputCreate, QuizAnswerRelationsInputCreate],
  { additionalProperties: false },
);

export const QuizAnswerInputUpdate = t.Composite(
  [QuizAnswerPlainInputUpdate, QuizAnswerRelationsInputUpdate],
  { additionalProperties: false },
);
