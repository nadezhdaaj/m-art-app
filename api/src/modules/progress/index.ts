import { Elysia } from "elysia";
import { ErrorDto } from "@/lib/http";
import { protectedPlugin } from "../auth";
import { progressModels } from "./model";
import { ProgressService } from "./service";

export const progressPlugin = new Elysia({
  prefix: "/progress",
  detail: { tags: ["Progress"] },
})
  .use(protectedPlugin)
  .get("/", ({ user }) => ProgressService.getProgress(user.id), {
    response: {
      200: progressModels.ProgressResponse,
      404: ErrorDto,
    },
    detail: {
      summary: "Get aggregated user progress",
    },
  })
  .get("/achievements", ({ user }) => ProgressService.getAchievements(user.id), {
    response: {
      200: progressModels.AchievementList,
      404: ErrorDto,
    },
    detail: {
      summary: "List unlocked achievements",
    },
  });
