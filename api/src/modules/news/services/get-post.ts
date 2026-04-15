import { prisma } from "@/lib/db";
import { ERROR_CODES } from "@/lib/http";
import { status } from "elysia";
import { newsPostDetailSelect } from "../select";
import { toSummary } from "../mapper";

export const getPost = async (slug: string) => {
  const post = await prisma.newsPost.findFirst({
    where: {
      slug,
      status: "PUBLISHED",
      publishedAt: { not: null },
    },
    select: newsPostDetailSelect,
  });

  if (!post) {
    throw status(404, {
      code: ERROR_CODES.NEWS_POST_NOT_FOUND,
      message: "News post was not found.",
    });
  }

  return {
    ...toSummary(post),
    content: post.content,
    sourceType: post.sourceType,
    sourceUrl: post.sourceUrl,
    sourceExternalId: post.sourceExternalId,
    createdAt: post.createdAt.toISOString(),
    updatedAt: post.updatedAt.toISOString(),
  };
};
