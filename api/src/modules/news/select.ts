export const newsPostSummarySelect = {
  id: true,
  slug: true,
  title: true,
  excerpt: true,
  coverImageUrl: true,
  publishedAt: true,
} as const;

export const newsPostDetailSelect = {
  ...newsPostSummarySelect,
  content: true,
  sourceType: true,
  sourceUrl: true,
  sourceExternalId: true,
  createdAt: true,
  updatedAt: true,
} as const;
