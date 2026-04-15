import { Elysia } from "elysia";
import { ErrorDto } from "@/lib/http";
import { protectedPlugin } from "../auth";
import { ProfileModels } from "./model";
import { getOverview } from "./services/get-overview";
import { getProfile, updateProfile } from "./services";

export const profilePlugin = new Elysia({ prefix: "/profile", detail: { tags: ["Profile"] } })
  .use(protectedPlugin)
  .get("/me", ({ user }) => getProfile(user.id), {
    response: {
      200: ProfileModels.Profile,
      404: ErrorDto,
    },
    detail: {
      summary: "Get my profile",
    },
  })
  .get("/me/overview", ({ user }) => getOverview(user.id), {
    response: {
      200: ProfileModels.ProfileOverview,
      404: ErrorDto,
    },
    detail: {
      summary: "Get my profile overview with progression",
    },
  })
  .patch("/me", ({ user, body }) => updateProfile(user.id, body), {
    body: ProfileModels.UpdateProfileBody,
    response: {
      200: ProfileModels.Profile,
      400: ErrorDto,
    },
    detail: {
      summary: "Create or update my profile",
    },
  });
