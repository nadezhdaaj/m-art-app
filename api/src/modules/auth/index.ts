import { Elysia } from "elysia";
import { ERROR_CODES } from "@/lib/http";
import { auth } from "./service";

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

let _schema: ReturnType<typeof auth.api.generateOpenAPISchema>;
const getSchema = async () => (_schema ??= auth.api.generateOpenAPISchema());
export const AuthOpenAPI = {
  getPaths: (prefix = "/auth") =>
    getSchema().then(({ paths }) => {
      const reference: typeof paths = Object.create(null);
      for (const path of Object.keys(paths)) {
        const key = prefix + path;
        reference[key] = paths[path];
        for (const method of Object.keys(paths[path])) {
          const operation = (reference[key] as any)[method];
          operation.tags = ["Auth"];
        }
      }
      return reference;
    }) as Promise<any>,
  components: getSchema().then(({ components }) => components) as Promise<any>,
} as const;
