import { t } from "elysia";
import { __nullable__ } from "./__nullable__";
import { __transformDate__ } from "./__transformDate__";

export const ProfilePlain = t.Object(
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
);

export const ProfileRelations = t.Object(
  {
    user: t.Object(
      {
        id: t.String(),
        name: t.String(),
        email: t.String(),
        emailVerified: t.Boolean(),
        image: __nullable__(t.String()),
        createdAt: t.Date(),
        updatedAt: t.Date(),
      },
      { additionalProperties: false },
    ),
    achievements: t.Array(
      t.Object(
        {
          id: t.String(),
          profileId: t.String(),
          achievementId: t.String(),
          unlockedAt: t.Date(),
          progress: t.Integer(),
          metadata: __nullable__(t.Any()),
          createdAt: t.Date(),
          updatedAt: t.Date(),
        },
        { additionalProperties: false },
      ),
      { additionalProperties: false },
    ),
    quizAttempts: t.Array(
      t.Object(
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
      { additionalProperties: false },
    ),
    progressEvents: t.Array(
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
          payload: __nullable__(t.Any()),
          idempotencyKey: t.String(),
          createdAt: t.Date(),
        },
        { additionalProperties: false },
      ),
      { additionalProperties: false },
    ),
  },
  { additionalProperties: false },
);

export const ProfilePlainInputCreate = t.Object(
  {
    displayName: t.Optional(__nullable__(t.String())),
    avatarUrl: t.Optional(__nullable__(t.String())),
    xp: t.Optional(t.Integer()),
  },
  { additionalProperties: false },
);

export const ProfilePlainInputUpdate = t.Object(
  {
    displayName: t.Optional(__nullable__(t.String())),
    avatarUrl: t.Optional(__nullable__(t.String())),
    xp: t.Optional(t.Integer()),
  },
  { additionalProperties: false },
);

export const ProfileRelationsInputCreate = t.Object(
  {
    user: t.Object(
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
    achievements: t.Optional(
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
    quizAttempts: t.Optional(
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
    progressEvents: t.Optional(
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

export const ProfileRelationsInputUpdate = t.Partial(
  t.Object(
    {
      user: t.Object(
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
      achievements: t.Partial(
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
      quizAttempts: t.Partial(
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
      progressEvents: t.Partial(
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

export const ProfileWhere = t.Partial(
  t.Recursive(
    (Self) =>
      t.Object(
        {
          AND: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          NOT: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          OR: t.Array(Self, { additionalProperties: false }),
          id: t.String(),
          userId: t.String(),
          displayName: t.String(),
          avatarUrl: t.String(),
          xp: t.Integer(),
          createdAt: t.Date(),
          updatedAt: t.Date(),
        },
        { additionalProperties: false },
      ),
    { $id: "Profile" },
  ),
);

export const ProfileWhereUnique = t.Recursive(
  (Self) =>
    t.Intersect(
      [
        t.Partial(
          t.Object({ id: t.String(), userId: t.String() }, { additionalProperties: false }),
          { additionalProperties: false },
        ),
        t.Union([t.Object({ id: t.String() }), t.Object({ userId: t.String() })], {
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
              userId: t.String(),
              displayName: t.String(),
              avatarUrl: t.String(),
              xp: t.Integer(),
              createdAt: t.Date(),
              updatedAt: t.Date(),
            },
            { additionalProperties: false },
          ),
        ),
      ],
      { additionalProperties: false },
    ),
  { $id: "Profile" },
);

export const ProfileSelect = t.Partial(
  t.Object(
    {
      id: t.Boolean(),
      userId: t.Boolean(),
      displayName: t.Boolean(),
      avatarUrl: t.Boolean(),
      xp: t.Boolean(),
      createdAt: t.Boolean(),
      updatedAt: t.Boolean(),
      user: t.Boolean(),
      achievements: t.Boolean(),
      quizAttempts: t.Boolean(),
      progressEvents: t.Boolean(),
      _count: t.Boolean(),
    },
    { additionalProperties: false },
  ),
);

export const ProfileInclude = t.Partial(
  t.Object(
    {
      user: t.Boolean(),
      achievements: t.Boolean(),
      quizAttempts: t.Boolean(),
      progressEvents: t.Boolean(),
      _count: t.Boolean(),
    },
    { additionalProperties: false },
  ),
);

export const ProfileOrderBy = t.Partial(
  t.Object(
    {
      id: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      userId: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      displayName: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      avatarUrl: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      xp: t.Union([t.Literal("asc"), t.Literal("desc")], {
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

export const Profile = t.Composite([ProfilePlain, ProfileRelations], {
  additionalProperties: false,
});

export const ProfileInputCreate = t.Composite(
  [ProfilePlainInputCreate, ProfileRelationsInputCreate],
  { additionalProperties: false },
);

export const ProfileInputUpdate = t.Composite(
  [ProfilePlainInputUpdate, ProfileRelationsInputUpdate],
  { additionalProperties: false },
);
