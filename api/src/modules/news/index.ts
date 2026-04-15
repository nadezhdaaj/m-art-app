import { Elysia } from "elysia";
import { ErrorDto } from "@/lib/http";
import { NewsModels } from "./model";
import { getPosts, getPost } from "./services";

export const newsPlugin = new Elysia({ prefix: "/news", detail: { tags: ["News"] } })
  .get("/", () => getPosts(), {
    response: {
      200: NewsModels.NewsPostList,
    },
    detail: {
      summary: "List published news posts",
    },
  })
  .get("/:slug", ({ params }) => getPost(params.slug), {
    response: {
      200: NewsModels.NewsPostDetail,
      404: ErrorDto,
    },
    detail: {
      summary: "Get published news post by slug",
    },
  });
