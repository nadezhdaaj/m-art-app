import { t, type UnwrapSchema } from "elysia";

const AchievementSummary = t.Object({
  id: t.String(),
  code: t.String(),
  name: t.String(),
  description: t.Nullable(t.String()),
  iconUrl: t.Nullable(t.String()),
  category: t.Nullable(t.String()),
  rarity: t.Nullable(t.String()),
  unlockedAt: t.String({ format: "date-time" }),
  progress: t.Integer(),
});
export type AchievementSummary = UnwrapSchema<typeof AchievementSummary>;

const RewardSummary = t.Object({
  gainedXp: t.Integer(),
  totalXp: t.Integer(),
  previousLevel: t.Integer(),
  currentLevel: t.Integer(),
  leveledUp: t.Boolean(),
  unlockedAchievements: t.Array(AchievementSummary),
});
export type RewardSummary = UnwrapSchema<typeof RewardSummary>;

const LevelProgress = t.Object({
  level: t.Integer(),
  currentLevelXp: t.Integer(),
  nextLevelXp: t.Integer(),
  xpIntoLevel: t.Integer(),
  xpToNextLevel: t.Integer(),
});

const ProgressProfile = t.Object({
  xp: t.Integer(),
  totalQuizCompletions: t.Integer(),
  totalQuizCorrectAnswers: t.Integer(),
  lastRewardedAt: t.Nullable(t.String({ format: "date-time" })),
});

const ProgressResponse = t.Composite([
  ProgressProfile,
  t.Object({
    levelProgress: LevelProgress,
    achievements: t.Array(AchievementSummary),
  }),
]);
export type ProgressResponse = UnwrapSchema<typeof ProgressResponse>;

export const ProgressModels = {
  AchievementSummary,
  RewardSummary,
  LevelProgress,
  ProgressProfile,
  ProgressResponse,
  AchievementList: t.Array(AchievementSummary),
} as const;
