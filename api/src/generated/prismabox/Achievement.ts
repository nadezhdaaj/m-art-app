import { t } from "elysia";
import { __nullable__ } from "./__nullable__";
import { __transformDate__ } from "./__transformDate__";

export const AchievementPlain = t.Object(
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
);

export const AchievementRelations = t.Object(
  {
    users: t.Array(
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
  },
  { additionalProperties: false },
);

export const AchievementPlainInputCreate = t.Object(
  {
    code: t.String(),
    name: t.String(),
    description: t.Optional(__nullable__(t.String())),
    iconUrl: t.Optional(__nullable__(t.String())),
    category: t.Optional(__nullable__(t.String())),
    rarity: t.Optional(__nullable__(t.String())),
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
    xpReward: t.Optional(t.Integer()),
    isHidden: t.Optional(t.Boolean()),
  },
  { additionalProperties: false },
);

export const AchievementPlainInputUpdate = t.Object(
  {
    code: t.Optional(t.String()),
    name: t.Optional(t.String()),
    description: t.Optional(__nullable__(t.String())),
    iconUrl: t.Optional(__nullable__(t.String())),
    category: t.Optional(__nullable__(t.String())),
    rarity: t.Optional(__nullable__(t.String())),
    ruleType: t.Optional(
      t.Union(
        [
          t.Literal("QUIZ_COMPLETIONS"),
          t.Literal("QUIZ_PERFECT_SCORE"),
          t.Literal("QUIZ_CORRECT_ANSWERS"),
          t.Literal("QUIZ_SINGLE_COMPLETION"),
        ],
        { additionalProperties: false },
      ),
    ),
    ruleConfig: t.Optional(t.Any()),
    xpReward: t.Optional(t.Integer()),
    isHidden: t.Optional(t.Boolean()),
  },
  { additionalProperties: false },
);

export const AchievementRelationsInputCreate = t.Object(
  {
    users: t.Optional(
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

export const AchievementRelationsInputUpdate = t.Partial(
  t.Object(
    {
      users: t.Partial(
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

export const AchievementWhere = t.Partial(
  t.Recursive(
    (Self) =>
      t.Object(
        {
          AND: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          NOT: t.Union([Self, t.Array(Self, { additionalProperties: false })]),
          OR: t.Array(Self, { additionalProperties: false }),
          id: t.String(),
          code: t.String(),
          name: t.String(),
          description: t.String(),
          iconUrl: t.String(),
          category: t.String(),
          rarity: t.String(),
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
    { $id: "Achievement" },
  ),
);

export const AchievementWhereUnique = t.Recursive(
  (Self) =>
    t.Intersect(
      [
        t.Partial(t.Object({ id: t.String(), code: t.String() }, { additionalProperties: false }), {
          additionalProperties: false,
        }),
        t.Union([t.Object({ id: t.String() }), t.Object({ code: t.String() })], {
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
              code: t.String(),
              name: t.String(),
              description: t.String(),
              iconUrl: t.String(),
              category: t.String(),
              rarity: t.String(),
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
        ),
      ],
      { additionalProperties: false },
    ),
  { $id: "Achievement" },
);

export const AchievementSelect = t.Partial(
  t.Object(
    {
      id: t.Boolean(),
      code: t.Boolean(),
      name: t.Boolean(),
      description: t.Boolean(),
      iconUrl: t.Boolean(),
      category: t.Boolean(),
      rarity: t.Boolean(),
      ruleType: t.Boolean(),
      ruleConfig: t.Boolean(),
      xpReward: t.Boolean(),
      isHidden: t.Boolean(),
      createdAt: t.Boolean(),
      updatedAt: t.Boolean(),
      users: t.Boolean(),
      _count: t.Boolean(),
    },
    { additionalProperties: false },
  ),
);

export const AchievementInclude = t.Partial(
  t.Object(
    { ruleType: t.Boolean(), users: t.Boolean(), _count: t.Boolean() },
    { additionalProperties: false },
  ),
);

export const AchievementOrderBy = t.Partial(
  t.Object(
    {
      id: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      code: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      name: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      description: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      iconUrl: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      category: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      rarity: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      ruleConfig: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      xpReward: t.Union([t.Literal("asc"), t.Literal("desc")], {
        additionalProperties: false,
      }),
      isHidden: t.Union([t.Literal("asc"), t.Literal("desc")], {
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

export const Achievement = t.Composite([AchievementPlain, AchievementRelations], {
  additionalProperties: false,
});

export const AchievementInputCreate = t.Composite(
  [AchievementPlainInputCreate, AchievementRelationsInputCreate],
  { additionalProperties: false },
);

export const AchievementInputUpdate = t.Composite(
  [AchievementPlainInputUpdate, AchievementRelationsInputUpdate],
  { additionalProperties: false },
);
