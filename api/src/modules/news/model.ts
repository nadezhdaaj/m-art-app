import { t, type UnwrapSchema } from "elysia";

const NewsPostSummary = t.Object({
  id: t.String(),
  slug: t.String(),
  title: t.String(),
  excerpt: t.Nullable(t.String()),
  coverImageUrl: t.Nullable(t.String()),
  publishedAt: t.String({ format: "date-time" }),
});
export type NewsPostSummary = UnwrapSchema<typeof NewsPostSummary>;

const NewsPostDetail = t.Composite([
  NewsPostSummary,
  t.Object({
    content: t.String(),
    sourceType: t.Union([t.Literal("MANUAL"), t.Literal("IMPORT")]),
    sourceUrl: t.Nullable(t.String()),
    sourceExternalId: t.Nullable(t.String()),
    createdAt: t.String({ format: "date-time" }),
    updatedAt: t.String({ format: "date-time" }),
  }),
]);
export type NewsPostDetail = UnwrapSchema<typeof NewsPostDetail>;

export const NewsModels = {
  NewsPostSummary,
  NewsPostDetail,
  NewsPostList: t.Array(NewsPostSummary),
} as const;
