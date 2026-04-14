import { Elysia } from "elysia";
import { ErrorDto } from "@/lib/http";
import { protectedPlugin } from "../auth";
import { QuizModels } from "./model";
import { QuizService } from "./service";

export const quizPlugin = new Elysia({ prefix: "/quizzes", detail: { tags: ["Quiz"] } })
  .use(protectedPlugin)
  .get("/", ({ user }) => QuizService.getQuizzes(user.id), {
    response: {
      200: QuizModels.QuizList,
    },
    detail: {
      summary: "List published quizzes",
    },
  })
  .get("/:quizId", ({ user, params }) => QuizService.getQuiz(user.id, params.quizId), {
    response: {
      200: QuizModels.QuizDetail,
      404: ErrorDto,
    },
    detail: {
      summary: "Get quiz details",
    },
  })
  .get(
    "/:quizId/results/:attemptId",
    ({ user, params }) => QuizService.getQuizResult(user.id, params.quizId, params.attemptId),
    {
      response: {
        200: QuizModels.QuizCompletionResult,
        404: ErrorDto,
      },
      detail: {
        summary: "Get completed quiz result",
      },
    },
  )
  .post(
    "/:quizId/questions/:questionId/answer",
    ({ user, params, body }) =>
      QuizService.submitAnswer(user.id, params.quizId, params.questionId, body.answerId),
    {
      body: QuizModels.QuizAnswerSubmit,
      response: {
        200: QuizModels.QuizAnswerResult,
        404: ErrorDto,
        409: ErrorDto,
      },
      detail: {
        summary: "Submit an answer for a quiz question",
      },
    },
  );
