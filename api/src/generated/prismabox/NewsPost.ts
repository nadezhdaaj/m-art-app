import { t } from "elysia";

import { __transformDate__ } from "./__transformDate__";

import { __nullable__ } from "./__nullable__";

export const NewsPostPlain = t.Object(
  {
    id: t.String(),
    slug: t.String(),
    title: t.String(),
    excerpt: __nullable__(t.String()),
    coverImageUrl: __nullable__(t.String()),
    content: t.String(),
    status: t.Union(
      [t.Literal("DRAFT"), t.Literal("PUBLISHED"), t.Literal("ARCHIVED")],
      { additionalProperties: false },
    ),
    sourceType: t.Union([t.Literal("MANUAL"), t.Literal("IMPORT")], {
      additionalProperties: false,
    }),
    sourceUrl: __nullable__(t.String()),
    sourceExternalId: __nullable__(t.String()),
    publishedAt: __nullable__(t.Date()),
    createdAt: t.Date(),
    updatedAt: t.Date(),
  },
  { additionalProperties: false },
);

export const NewsPostRelations = t.Object({}, { additionalProperties: false });

export const NewsPostPlainInputCreate = t.Object(
  {
    slug: t.String(),
    title: t.String(),
    excerpt: t.Optional(__nullable__(t.String())),
    coverImageUrl: t.Optional(__nullable__(t.String())),
    content: t.String(),
    status: t.Optional(
      t.Union(
        [t.Literal("DRAFT"), t.Literal("PUBLISHED"), t.Literal("ARCHIVED")],
        { additionalProperties: false },
      ),
    ),
    sourceType: t.Optional(
      t.Union([t.Literal("MANUAL"), t.Literal("IMPORT")], {
        additionalProperties: false,
      }),
    ),
    sourceUrl: t.Optional(__nullable__(t.String())),
    publishedAt: t.Optional(__nullable__(t.Date())),
  },
  { additionalProperties: false },
);

export const NewsPostPlainInputUpdate = t.Object(
  {
    slug: t.Optional(t.String()),
    title: t.Optional(t.String()),
    excerpt: t.Optional(__nullable__(t.String())),
    coverImageUrl: t.Optional(__nullable__(t.String())),
    content: t.Optional(t.String()),
    status: t.Optional(
      t.Union(
        [t.Literal("DRAFT"), t.Literal("PUBLISHED"), t.Literal("ARCHIVED")],
        { additionalProperties: false },
      ),
    ),
    sourceType: t.Optional(
      t.Union([t.Literal("MANUAL"), t.Literal("IMPORT")], {
        additionalProperties: false,
      }),
    ),
    sourceUrl: t.Optional(__nullable__(t.String())),
    publishedAt: t.Optional(__nullable__(t.Date())),
  },
  { additionalProperties: false },
);

export const NewsPostRelationsInputCreate = t.Object(
  {},
  { additionalProperties: false },
);

export const NewsPostRelationsInputUpdate = t.Partial(
  t.Object({}, { additionalProperties: false }),
);

export const NewsPostWhere = t.Partial(
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
          excerpt: t.String(),
          coverImageUrl: t.String(),
          content: t.String(),
          status: t.Union(
            [t.Literal("DRAFT"), t.Literal("PUBLISHED"), t.Literal("ARCHIVED")],
            { additionalProperties: false },
          ),
          sourceType: t.Union([t.Literal("MANUAL"), t.Literal("IMPORT")], {
            additionalProperties: false,
          }),
          sourceUrl: t.String(),
          sourceExternalId: t.String(),
          publishedAt: t.Date(),
          createdAt: t.Date(),
          updatedAt: t.Date(),
        },
        { additionalProperties: false },
      ),
    { $id: "NewsPost" },
  ),
);

export const NewsPostWhereUnique = t.Recursive(
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
              excerpt: t.String(),
              coverImageUrl: t.String(),
              content: t.String(),
              status: t.Union(
                [
                  t.Literal("DRAFT"),
                  t.Literal("PUBLISHED"),
                  t.Literal("ARCHIVED"),
                ],
                { additionalProperties: false },
              ),
              sourceType: t.Union([t.Literal("MANUAL"), t.Literal("IMPORT")], {
                additionalProperties: false,
              }),
              sourceUrl: t.String(),
              sourceExternalId: t.String(),
              publishedAt: t.Date(),
              createdAt: t.Date(),
              updatedAt: t.Date(),
            },
            { additionalProperties: false },
          ),
        ),
      ],
      { additionalProperties: false },
    ),
  { $id: "NewsPost" },
);

export const NewsPostSelect = t.Partial(
  t.Object(
    {
      id: t.Boolean(),
      slug: t.Boolean(),
      title: t.Boolean(),
      excerpt: t.Boolean(),
      coverImageUrl: t.Boolean(),
      content: t.Boolean(),
      status: t.Boolean(),
      sourceType: t.Boolean(),
      sourceUrl: t.Boolean(),
      sourceExternalId: t.Boolean(),
      publishedAt: t.Boolean(),
      createdAt: t.Boolean(),
      updatedAt: t.Boolean(),
      _count: t.Boolean(),
    },
    { additionalProperties: false },
  ),
);

export const NewsPostInclude = t.Partial(
  t.Object(
    { status: t.Boolean(), sourceType: t.Boolean(), _count: t.Boolean() },
    { additionalProperties: false },
  ),
);

export const NewsPostOrderBy = t.Partial(
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
      excerpt: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      coverImageUrl: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      content: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      sourceUrl: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      sourceExternalId: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      publishedAt: t.Union([t.Literal("asc"), t.Literal("desc")], {
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

export const NewsPost = t.Composite([NewsPostPlain, NewsPostRelations], {
  additionalProperties: false,
});

export const NewsPostInputCreate = t.Composite(
  [NewsPostPlainInputCreate, NewsPostRelationsInputCreate],
  { additionalProperties: false },
);

export const NewsPostInputUpdate = t.Composite(
  [NewsPostPlainInputUpdate, NewsPostRelationsInputUpdate],
  { additionalProperties: false },
);
