import { Elysia } from "elysia";
import { ErrorDto } from "@/lib/http";
import { protectedPlugin } from "../auth";
import { ProgressModels } from "./model";
import { getProgress, getAchievements } from "./services";

export const progressPlugin = new Elysia({ prefix: "/progress", detail: { tags: ["Progress"] } })
  .use(protectedPlugin)
  .get("/", ({ user }) => getProgress(user.id), {
    response: {
      200: ProgressModels.ProgressResponse,
      404: ErrorDto,
    },
    detail: {
      summary: "Get aggregated user progress",
    },
  })
  .get("/achievements", ({ user }) => getAchievements(user.id), {
    response: {
      200: ProgressModels.AchievementList,
      404: ErrorDto,
    },
    detail: {
      summary: "List unlocked achievements",
    },
  });
