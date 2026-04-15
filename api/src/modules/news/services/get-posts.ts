import { prisma } from "@/lib/db";
import { newsPostSummarySelect } from "../select";
import { toSummary } from "../mapper";

export const getPosts = async () => {
  const posts = await prisma.newsPost.findMany({
    where: {
      status: "PUBLISHED",
      publishedAt: { not: null },
    },
    select: newsPostSummarySelect,
    orderBy: [{ publishedAt: "desc" }, { createdAt: "desc" }],
  });

  return posts.map(toSummary);
};
