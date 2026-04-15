import { t } from "elysia";

import { __transformDate__ } from "./__transformDate__";

import { __nullable__ } from "./__nullable__";

export const QuizPlain = t.Object(
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
);

export const QuizRelations = t.Object(
  {
    questions: t.Array(
      t.Object(
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
      { additionalProperties: false },
    ),
    imagePool: t.Array(
      t.Object(
        {
          id: t.String(),
          quizId: t.String(),
          imageUrl: t.String(),
          sortOrder: t.Integer(),
          createdAt: t.Date(),
          updatedAt: t.Date(),
        },
        { additionalProperties: false },
      ),
      { additionalProperties: false },
    ),
    attempts: t.Array(
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
          questionOrderIds: __nullable__(t.Any()),
          answeredQuestionIds: __nullable__(t.Any()),
          startedAt: t.Date(),
          finishedAt: __nullable__(t.Date()),
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

export const QuizPlainInputCreate = t.Object(
  {
    slug: t.String(),
    title: t.String(),
    description: t.Optional(__nullable__(t.String())),
    previewImageUrl: t.Optional(__nullable__(t.String())),
    type: t.Optional(
      t.Union([t.Literal("MULTIPLE_CHOICE"), t.Literal("YES_NO")], {
        additionalProperties: false,
      }),
    ),
    status: t.Optional(
      t.Union(
        [t.Literal("DRAFT"), t.Literal("PUBLISHED"), t.Literal("ARCHIVED")],
        { additionalProperties: false },
      ),
    ),
  },
  { additionalProperties: false },
);

export const QuizPlainInputUpdate = t.Object(
  {
    slug: t.Optional(t.String()),
    title: t.Optional(t.String()),
    description: t.Optional(__nullable__(t.String())),
    previewImageUrl: t.Optional(__nullable__(t.String())),
    type: t.Optional(
      t.Union([t.Literal("MULTIPLE_CHOICE"), t.Literal("YES_NO")], {
        additionalProperties: false,
      }),
    ),
    status: t.Optional(
      t.Union(
        [t.Literal("DRAFT"), t.Literal("PUBLISHED"), t.Literal("ARCHIVED")],
        { additionalProperties: false },
      ),
    ),
  },
  { additionalProperties: false },
);

export const QuizRelationsInputCreate = t.Object(
  {
    questions: t.Optional(
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
    imagePool: t.Optional(
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
    attempts: t.Optional(
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

export const QuizRelationsInputUpdate = t.Partial(
  t.Object(
    {
      questions: t.Partial(
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
      imagePool: t.Partial(
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
      attempts: t.Partial(
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

export const QuizWhere = t.Partial(
  t.Recursive(
    (Self) =>
      t.Object(
        {
          AND: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          NOT: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          OR: t.Array(Self, { additionalProperties: false }),
          id: t.String(),
          slug: t.String(),
          title: t.String(),
          description: t.String(),
          previewImageUrl: t.String(),
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
    { $id: "Quiz" },
  ),
);

export const QuizWhereUnique = t.Recursive(
  (Self) =>
    t.Intersect(
      [
        t.Partial(
          t.Object(
            { id: t.String(), slug: t.String() },
            { additionalProperties: false },
          ),
          { additionalProperties: false },
        ),
        t.Union(
          [t.Object({ id: t.String() }), t.Object({ slug: t.String() })],
          { additionalProperties: false },
        ),
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
              slug: t.String(),
              title: t.String(),
              description: t.String(),
              previewImageUrl: t.String(),
              type: t.Union(
                [t.Literal("MULTIPLE_CHOICE"), t.Literal("YES_NO")],
                { additionalProperties: false },
              ),
              status: t.Union(
                [
                  t.Literal("DRAFT"),
                  t.Literal("PUBLISHED"),
                  t.Literal("ARCHIVED"),
                ],
                { additionalProperties: false },
              ),
              createdAt: t.Date(),
              updatedAt: t.Date(),
            },
            { additionalProperties: false },
          ),
        ),
      ],
      { additionalProperties: false },
    ),
  { $id: "Quiz" },
);

export const QuizSelect = t.Partial(
  t.Object(
    {
      id: t.Boolean(),
      slug: t.Boolean(),
      title: t.Boolean(),
      description: t.Boolean(),
      previewImageUrl: t.Boolean(),
      type: t.Boolean(),
      status: t.Boolean(),
      createdAt: t.Boolean(),
      updatedAt: t.Boolean(),
      questions: t.Boolean(),
      imagePool: t.Boolean(),
      attempts: t.Boolean(),
      _count: t.Boolean(),
    },
    { additionalProperties: false },
  ),
);

export const QuizInclude = t.Partial(
  t.Object(
    {
      type: t.Boolean(),
      status: t.Boolean(),
      questions: t.Boolean(),
      imagePool: t.Boolean(),
      attempts: t.Boolean(),
      _count: t.Boolean(),
    },
    { additionalProperties: false },
  ),
);

export const QuizOrderBy = t.Partial(
  t.Object(
    {
      id: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      slug: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      title: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      description: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      previewImageUrl: t.Union([t.Literal("asc"), t.Literal("desc")], {
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

export const Quiz = t.Composite([QuizPlain, QuizRelations], {
  additionalProperties: false,
});

export const QuizInputCreate = t.Composite(
  [QuizPlainInputCreate, QuizRelationsInputCreate],
  { additionalProperties: false },
);

export const QuizInputUpdate = t.Composite(
  [QuizPlainInputUpdate, QuizRelationsInputUpdate],
  { additionalProperties: false },
);
