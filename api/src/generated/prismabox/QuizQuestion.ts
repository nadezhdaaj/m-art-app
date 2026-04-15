import { t } from "elysia";
import { __nullable__ } from "./__nullable__";
import { __transformDate__ } from "./__transformDate__";

export const QuizQuestionPlain = t.Object(
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
);

export const QuizQuestionRelations = t.Object(
  {
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
        status: t.Union([t.Literal("DRAFT"), t.Literal("PUBLISHED"), t.Literal("ARCHIVED")], {
          additionalProperties: false,
        }),
        createdAt: t.Date(),
        updatedAt: t.Date(),
      },
      { additionalProperties: false },
    ),
    answers: t.Array(
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
      { additionalProperties: false },
    ),
  },
  { additionalProperties: false },
);

export const QuizQuestionPlainInputCreate = t.Object(
  {
    text: t.String(),
    fact: t.String(),
    imageUrl: t.Optional(__nullable__(t.String())),
  },
  { additionalProperties: false },
);

export const QuizQuestionPlainInputUpdate = t.Object(
  {
    text: t.Optional(t.String()),
    fact: t.Optional(t.String()),
    imageUrl: t.Optional(__nullable__(t.String())),
  },
  { additionalProperties: false },
);

export const QuizQuestionRelationsInputCreate = t.Object(
  {
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

export const QuizQuestionRelationsInputUpdate = t.Partial(
  t.Object(
    {
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

export const QuizQuestionWhere = t.Partial(
  t.Recursive(
    (Self) =>
      t.Object(
        {
          AND: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          NOT: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          OR: t.Array(Self, { additionalProperties: false }),
          id: t.String(),
          quizId: t.String(),
          text: t.String(),
          fact: t.String(),
          imageUrl: t.String(),
          createdAt: t.Date(),
          updatedAt: t.Date(),
        },
        { additionalProperties: false },
      ),
    { $id: "QuizQuestion" },
  ),
);

export const QuizQuestionWhereUnique = t.Recursive(
  (Self) =>
    t.Intersect(
      [
        t.Partial(t.Object({ id: t.String() }, { additionalProperties: false }), {
          additionalProperties: false,
        }),
        t.Union([t.Object({ id: t.String() })], {
          additionalProperties: false,
        }),
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
              quizId: t.String(),
              text: t.String(),
              fact: t.String(),
              imageUrl: t.String(),
              createdAt: t.Date(),
              updatedAt: t.Date(),
            },
            { additionalProperties: false },
          ),
        ),
      ],
      { additionalProperties: false },
    ),
  { $id: "QuizQuestion" },
);

export const QuizQuestionSelect = t.Partial(
  t.Object(
    {
      id: t.Boolean(),
      quizId: t.Boolean(),
      text: t.Boolean(),
      fact: t.Boolean(),
      imageUrl: t.Boolean(),
      createdAt: t.Boolean(),
      updatedAt: t.Boolean(),
      quiz: t.Boolean(),
      answers: t.Boolean(),
      _count: t.Boolean(),
    },
    { additionalProperties: false },
  ),
);

export const QuizQuestionInclude = t.Partial(
  t.Object(
    { quiz: t.Boolean(), answers: t.Boolean(), _count: t.Boolean() },
    { additionalProperties: false },
  ),
);

export const QuizQuestionOrderBy = t.Partial(
  t.Object(
    {
      id: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      quizId: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      text: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      fact: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      imageUrl: t.Union([t.Literal("asc"), t.Literal("desc")], {
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

export const QuizQuestion = t.Composite([QuizQuestionPlain, QuizQuestionRelations], {
  additionalProperties: false,
});

export const QuizQuestionInputCreate = t.Composite(
  [QuizQuestionPlainInputCreate, QuizQuestionRelationsInputCreate],
  { additionalProperties: false },
);

export const QuizQuestionInputUpdate = t.Composite(
  [QuizQuestionPlainInputUpdate, QuizQuestionRelationsInputUpdate],
  { additionalProperties: false },
);
