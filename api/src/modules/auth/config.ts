import { prisma } from "@/lib/db";
import { prismaAdapter } from "@better-auth/prisma-adapter";
import { betterAuth } from "better-auth";
import { bearer, openAPI } from "better-auth/plugins";
import { setupUserProfile } from "./services/setupUserProfile";

const BETTER_AUTH_URL = process.env.BETTER_AUTH_URL || "http://localhost:3000";
const BETTER_AUTH_SECRET = process.env.BETTER_AUTH_SECRET;

if (!BETTER_AUTH_SECRET) {
  throw Error("BETTER_AUTH_SECRET is not set");
}

export const auth = betterAuth({
  database: prismaAdapter(prisma, {
    provider: "postgresql",
  }),
  baseURL: BETTER_AUTH_URL,
  basePath: "/auth",
  trustedOrigins: [BETTER_AUTH_URL],
  secret: BETTER_AUTH_SECRET,
  emailAndPassword: {
    enabled: true,
    requireEmailVerification: false,
  },
  plugins: [openAPI(), bearer()],
  databaseHooks: {
    user: {
      create: {
        after: setupUserProfile,
      },
    },
  },
});
