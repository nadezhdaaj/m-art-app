import { prismaAdapter } from "@better-auth/prisma-adapter";
import { betterAuth } from "better-auth";
import { bearer, openAPI } from "better-auth/plugins";
import { prisma } from "@/lib/db";
import { completeRegister } from "./complete-register";

const APP_URL = process.env.BETTER_AUTH_URL || "http://localhost:3000";

export const auth = betterAuth({
  database: prismaAdapter(prisma, {
    provider: "postgresql",
  }),
  baseURL: APP_URL,
  basePath: "/auth",
  trustedOrigins: [APP_URL],
  secret: process.env.BETTER_AUTH_SECRET,
  emailAndPassword: {
    enabled: true,
    requireEmailVerification: false,
  },
  plugins: [openAPI(), bearer()],
  databaseHooks: {
    user: {
      create: {
        after: completeRegister,
      },
    },
  },
});
