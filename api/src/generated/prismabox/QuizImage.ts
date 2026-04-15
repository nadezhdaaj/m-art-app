import { t } from "elysia";
import { __nullable__ } from "./__nullable__";
import { __transformDate__ } from "./__transformDate__";

export const QuizImagePlain = t.Object(
  {
    id: t.String(),
    quizId: t.String(),
    imageUrl: t.String(),
    sortOrder: t.Integer(),
    createdAt: t.Date(),
    updatedAt: t.Date(),
  },
  { additionalProperties: false },
);

export const QuizImageRelations = t.Object(
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
  },
  { additionalProperties: false },
);

export const QuizImagePlainInputCreate = t.Object(
  { imageUrl: t.String(), sortOrder: t.Optional(t.Integer()) },
  { additionalProperties: false },
);

export const QuizImagePlainInputUpdate = t.Object(
  { imageUrl: t.Optional(t.String()), sortOrder: t.Optional(t.Integer()) },
  { additionalProperties: false },
);

export const QuizImageRelationsInputCreate = t.Object(
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
  },
  { additionalProperties: false },
);

export const QuizImageRelationsInputUpdate = t.Partial(
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
    },
    { additionalProperties: false },
  ),
);

export const QuizImageWhere = t.Partial(
  t.Recursive(
    (Self) =>
      t.Object(
        {
          AND: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          NOT: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          OR: t.Array(Self, { additionalProperties: false }),
          id: t.String(),
          quizId: t.String(),
          imageUrl: t.String(),
          sortOrder: t.Integer(),
          createdAt: t.Date(),
          updatedAt: t.Date(),
        },
        { additionalProperties: false },
      ),
    { $id: "QuizImage" },
  ),
);

export const QuizImageWhereUnique = t.Recursive(
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
              imageUrl: t.String(),
              sortOrder: t.Integer(),
              createdAt: t.Date(),
              updatedAt: t.Date(),
            },
            { additionalProperties: false },
          ),
        ),
      ],
      { additionalProperties: false },
    ),
  { $id: "QuizImage" },
);

export const QuizImageSelect = t.Partial(
  t.Object(
    {
      id: t.Boolean(),
      quizId: t.Boolean(),
      imageUrl: t.Boolean(),
      sortOrder: t.Boolean(),
      createdAt: t.Boolean(),
      updatedAt: t.Boolean(),
      quiz: t.Boolean(),
      _count: t.Boolean(),
    },
    { additionalProperties: false },
  ),
);

export const QuizImageInclude = t.Partial(
  t.Object({ quiz: t.Boolean(), _count: t.Boolean() }, { additionalProperties: false }),
);

export const QuizImageOrderBy = t.Partial(
  t.Object(
    {
      id: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      quizId: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      imageUrl: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      sortOrder: t.Union([t.Literal("asc"), t.Literal("desc")], {
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

export const QuizImage = t.Composite([QuizImagePlain, QuizImageRelations], {
  additionalProperties: false,
});

export const QuizImageInputCreate = t.Composite(
  [QuizImagePlainInputCreate, QuizImageRelationsInputCreate],
  { additionalProperties: false },
);

export const QuizImageInputUpdate = t.Composite(
  [QuizImagePlainInputUpdate, QuizImageRelationsInputUpdate],
  { additionalProperties: false },
);
