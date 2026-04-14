import { Elysia } from "elysia";
import { ErrorDto } from "@/lib/http";
import { protectedPlugin } from "../auth";
import { profileModels } from "./model";
import { ProfileService } from "./service";

export const profilePlugin = new Elysia({ prefix: "/profile", detail: { tags: ["Profile"] } })
  .use(protectedPlugin)
  .get("/me", ({ user }) => ProfileService.getProfile(user.id), {
    response: {
      200: profileModels.profile,
      404: ErrorDto,
    },
    detail: {
      summary: "Get my profile",
    },
  })
  .patch("/me", ({ user, body }) => ProfileService.updateProfile(user.id, body), {
    body: profileModels.updateProfileBody,
    response: {
      200: profileModels.profile,
      400: ErrorDto,
    },
    detail: {
      summary: "Create or update my profile",
    },
  });
