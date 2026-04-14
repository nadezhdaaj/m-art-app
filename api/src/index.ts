import openapi from "@elysiajs/openapi";
import { Elysia } from "elysia";
import { AuthOpenAPI, authPlugin } from "./modules/auth";
import { profilePlugin } from "./modules/profile";
import { progressPlugin } from "./modules/progress";
import { quizPlugin } from "./modules/quiz";

const APP_PORT = process.env.PORT || 3000;

const app = new Elysia()
  .use(
    openapi({
      scalar: {
        theme: "deepSpace",
        customCss: "",
      },
      documentation: {
        components: await AuthOpenAPI.components,
        paths: await AuthOpenAPI.getPaths(),
      },
    }),
  )
  .use(authPlugin)
  .use(profilePlugin)
  .use(progressPlugin)
  .use(quizPlugin)
  .listen(APP_PORT);

console.log(`🦊 Elysia is running at http://${app.server?.hostname}:${app.server?.port}`);
