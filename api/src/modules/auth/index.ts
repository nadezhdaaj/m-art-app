import { Elysia } from "elysia";
import { ERROR_CODES } from "@/lib/http";
import { auth } from "./config";
export { AuthOpenAPI } from "./openapi";

export const authPlugin = new Elysia({ name: "auth" }).mount(auth.handler);

export const protectedPlugin = new Elysia({ name: "protected" }).derive(
  { as: "scoped" },
  async ({ request, status }) => {
    const session = await auth.api.getSession({
      headers: request.headers,
    });

    if (!session) {
      throw status(401, {
        code: ERROR_CODES.AUTH_UNAUTHORIZED,
        message: "Authorization is required for this endpoint.",
      });
    }

    return {
      user: session.user,
      session: session.session,
    };
  },
);
