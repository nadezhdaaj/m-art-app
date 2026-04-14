import { t } from "elysia";

import { __transformDate__ } from "./__transformDate__";

import { __nullable__ } from "./__nullable__";

export const UserAchievementPlain = t.Object(
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
);

export const UserAchievementRelations = t.Object(
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
    achievement: t.Object(
      {
        id: t.String(),
        code: t.String(),
        name: t.String(),
        description: __nullable__(t.String()),
        iconUrl: __nullable__(t.String()),
        category: __nullable__(t.String()),
        rarity: __nullable__(t.String()),
        ruleType: t.Union(
          [
            t.Literal("QUIZ_COMPLETIONS"),
            t.Literal("QUIZ_PERFECT_SCORE"),
            t.Literal("QUIZ_CORRECT_ANSWERS"),
            t.Literal("QUIZ_SINGLE_COMPLETION"),
          ],
          { additionalProperties: false },
        ),
        ruleConfig: t.Any(),
        xpReward: t.Integer(),
        isHidden: t.Boolean(),
        createdAt: t.Date(),
        updatedAt: t.Date(),
      },
      { additionalProperties: false },
    ),
  },
  { additionalProperties: false },
);

export const UserAchievementPlainInputCreate = t.Object(
  {
    unlockedAt: t.Optional(t.Date()),
    progress: t.Optional(t.Integer()),
    metadata: t.Optional(__nullable__(t.Any())),
  },
  { additionalProperties: false },
);

export const UserAchievementPlainInputUpdate = t.Object(
  {
    unlockedAt: t.Optional(t.Date()),
    progress: t.Optional(t.Integer()),
    metadata: t.Optional(__nullable__(t.Any())),
  },
  { additionalProperties: false },
);

export const UserAchievementRelationsInputCreate = t.Object(
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
    achievement: t.Object(
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

export const UserAchievementRelationsInputUpdate = t.Partial(
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
      achievement: t.Object(
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

export const UserAchievementWhere = t.Partial(
  t.Recursive(
    (Self) =>
      t.Object(
        {
          AND: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          NOT: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          OR: t.Array(Self, { additionalProperties: false }),
          id: t.String(),
          profileId: t.String(),
          achievementId: t.String(),
          unlockedAt: t.Date(),
          progress: t.Integer(),
          metadata: t.Any(),
          createdAt: t.Date(),
          updatedAt: t.Date(),
        },
        { additionalProperties: false },
      ),
    { $id: "UserAchievement" },
  ),
);

export const UserAchievementWhereUnique = t.Recursive(
  (Self) =>
    t.Intersect(
      [
        t.Partial(
          t.Object(
            {
              id: t.String(),
              profileId_achievementId: t.Object(
                { profileId: t.String(), achievementId: t.String() },
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
              profileId_achievementId: t.Object(
                { profileId: t.String(), achievementId: t.String() },
                { additionalProperties: false },
              ),
            }),
          ],
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
              profileId: t.String(),
              achievementId: t.String(),
              unlockedAt: t.Date(),
              progress: t.Integer(),
              metadata: t.Any(),
              createdAt: t.Date(),
              updatedAt: t.Date(),
            },
            { additionalProperties: false },
          ),
        ),
      ],
      { additionalProperties: false },
    ),
  { $id: "UserAchievement" },
);

export const UserAchievementSelect = t.Partial(
  t.Object(
    {
      id: t.Boolean(),
      profileId: t.Boolean(),
      achievementId: t.Boolean(),
      unlockedAt: t.Boolean(),
      progress: t.Boolean(),
      metadata: t.Boolean(),
      createdAt: t.Boolean(),
      updatedAt: t.Boolean(),
      profile: t.Boolean(),
      achievement: t.Boolean(),
      _count: t.Boolean(),
    },
    { additionalProperties: false },
  ),
);

export const UserAchievementInclude = t.Partial(
  t.Object(
    { profile: t.Boolean(), achievement: t.Boolean(), _count: t.Boolean() },
    { additionalProperties: false },
  ),
);

export const UserAchievementOrderBy = t.Partial(
  t.Object(
    {
      id: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      profileId: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      achievementId: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      unlockedAt: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      progress: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      metadata: t.Union([t.Literal("asc"), t.Literal("desc")], {
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

export const UserAchievement = t.Composite(
  [UserAchievementPlain, UserAchievementRelations],
  { additionalProperties: false },
);

export const UserAchievementInputCreate = t.Composite(
  [UserAchievementPlainInputCreate, UserAchievementRelationsInputCreate],
  { additionalProperties: false },
);

export const UserAchievementInputUpdate = t.Composite(
  [UserAchievementPlainInputUpdate, UserAchievementRelationsInputUpdate],
  { additionalProperties: false },
);
