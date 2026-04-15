import { t } from "elysia";
import { __nullable__ } from "./__nullable__";
import { __transformDate__ } from "./__transformDate__";

export const ProgressEventPlain = t.Object(
  {
    id: t.String(),
    profileId: t.String(),
    sourceType: t.Union(
      [
        t.Literal("QUIZ_ATTEMPT"),
        t.Literal("EXHIBIT_VIEW"),
        t.Literal("DAILY_LOGIN"),
        t.Literal("ARTWORK_CREATED"),
      ],
      { additionalProperties: false },
    ),
    sourceId: t.String(),
    eventType: t.Union([t.Literal("QUIZ_COMPLETED"), t.Literal("ACHIEVEMENT_UNLOCKED")], {
      additionalProperties: false,
    }),
    xpDelta: t.Integer(),
    payload: __nullable__(t.Any()),
    idempotencyKey: t.String(),
    createdAt: t.Date(),
  },
  { additionalProperties: false },
);

export const ProgressEventRelations = t.Object(
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
  },
  { additionalProperties: false },
);

export const ProgressEventPlainInputCreate = t.Object(
  {
    sourceType: t.Union(
      [
        t.Literal("QUIZ_ATTEMPT"),
        t.Literal("EXHIBIT_VIEW"),
        t.Literal("DAILY_LOGIN"),
        t.Literal("ARTWORK_CREATED"),
      ],
      { additionalProperties: false },
    ),
    eventType: t.Union([t.Literal("QUIZ_COMPLETED"), t.Literal("ACHIEVEMENT_UNLOCKED")], {
      additionalProperties: false,
    }),
    xpDelta: t.Optional(t.Integer()),
    payload: t.Optional(__nullable__(t.Any())),
    idempotencyKey: t.String(),
  },
  { additionalProperties: false },
);

export const ProgressEventPlainInputUpdate = t.Object(
  {
    sourceType: t.Optional(
      t.Union(
        [
          t.Literal("QUIZ_ATTEMPT"),
          t.Literal("EXHIBIT_VIEW"),
          t.Literal("DAILY_LOGIN"),
          t.Literal("ARTWORK_CREATED"),
        ],
        { additionalProperties: false },
      ),
    ),
    eventType: t.Optional(
      t.Union([t.Literal("QUIZ_COMPLETED"), t.Literal("ACHIEVEMENT_UNLOCKED")], {
        additionalProperties: false,
      }),
    ),
    xpDelta: t.Optional(t.Integer()),
    payload: t.Optional(__nullable__(t.Any())),
    idempotencyKey: t.Optional(t.String()),
  },
  { additionalProperties: false },
);

export const ProgressEventRelationsInputCreate = t.Object(
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
  },
  { additionalProperties: false },
);

export const ProgressEventRelationsInputUpdate = t.Partial(
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
    },
    { additionalProperties: false },
  ),
);

export const ProgressEventWhere = t.Partial(
  t.Recursive(
    (Self) =>
      t.Object(
        {
          AND: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          NOT: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          OR: t.Array(Self, { additionalProperties: false }),
          id: t.String(),
          profileId: t.String(),
          sourceType: t.Union(
            [
              t.Literal("QUIZ_ATTEMPT"),
              t.Literal("EXHIBIT_VIEW"),
              t.Literal("DAILY_LOGIN"),
              t.Literal("ARTWORK_CREATED"),
            ],
            { additionalProperties: false },
          ),
          sourceId: t.String(),
          eventType: t.Union([t.Literal("QUIZ_COMPLETED"), t.Literal("ACHIEVEMENT_UNLOCKED")], {
            additionalProperties: false,
          }),
          xpDelta: t.Integer(),
          payload: t.Any(),
          idempotencyKey: t.String(),
          createdAt: t.Date(),
        },
        { additionalProperties: false },
      ),
    { $id: "ProgressEvent" },
  ),
);

export const ProgressEventWhereUnique = t.Recursive(
  (Self) =>
    t.Intersect(
      [
        t.Partial(
          t.Object({ id: t.String(), idempotencyKey: t.String() }, { additionalProperties: false }),
          { additionalProperties: false },
        ),
        t.Union([t.Object({ id: t.String() }), t.Object({ idempotencyKey: t.String() })], {
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
              profileId: t.String(),
              sourceType: t.Union(
                [
                  t.Literal("QUIZ_ATTEMPT"),
                  t.Literal("EXHIBIT_VIEW"),
                  t.Literal("DAILY_LOGIN"),
                  t.Literal("ARTWORK_CREATED"),
                ],
                { additionalProperties: false },
              ),
              sourceId: t.String(),
              eventType: t.Union([t.Literal("QUIZ_COMPLETED"), t.Literal("ACHIEVEMENT_UNLOCKED")], {
                additionalProperties: false,
              }),
              xpDelta: t.Integer(),
              payload: t.Any(),
              idempotencyKey: t.String(),
              createdAt: t.Date(),
            },
            { additionalProperties: false },
          ),
        ),
      ],
      { additionalProperties: false },
    ),
  { $id: "ProgressEvent" },
);

export const ProgressEventSelect = t.Partial(
  t.Object(
    {
      id: t.Boolean(),
      profileId: t.Boolean(),
      sourceType: t.Boolean(),
      sourceId: t.Boolean(),
      eventType: t.Boolean(),
      xpDelta: t.Boolean(),
      payload: t.Boolean(),
      idempotencyKey: t.Boolean(),
      createdAt: t.Boolean(),
      profile: t.Boolean(),
      _count: t.Boolean(),
    },
    { additionalProperties: false },
  ),
);

export const ProgressEventInclude = t.Partial(
  t.Object(
    {
      sourceType: t.Boolean(),
      eventType: t.Boolean(),
      profile: t.Boolean(),
      _count: t.Boolean(),
    },
    { additionalProperties: false },
  ),
);

export const ProgressEventOrderBy = t.Partial(
  t.Object(
    {
      id: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      profileId: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      sourceId: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      xpDelta: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      payload: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      idempotencyKey: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      createdAt: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
    },
    { additionalProperties: false },
  ),
);

export const ProgressEvent = t.Composite([ProgressEventPlain, ProgressEventRelations], {
  additionalProperties: false,
});

export const ProgressEventInputCreate = t.Composite(
  [ProgressEventPlainInputCreate, ProgressEventRelationsInputCreate],
  { additionalProperties: false },
);

export const ProgressEventInputUpdate = t.Composite(
  [ProgressEventPlainInputUpdate, ProgressEventRelationsInputUpdate],
  { additionalProperties: false },
);
