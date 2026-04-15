export const toSummary = (post: {
  id: string;
  slug: string;
  title: string;
  excerpt: string | null;
  coverImageUrl: string | null;
  publishedAt: Date | null;
}) => ({
  id: post.id,
  slug: post.slug,
  title: post.title,
  excerpt: post.excerpt,
  coverImageUrl: post.coverImageUrl,
  publishedAt: post.publishedAt?.toISOString() ?? new Date(0).toISOString(),
});
