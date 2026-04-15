import { t } from "elysia";
import { __nullable__ } from "./__nullable__";
import { __transformDate__ } from "./__transformDate__";

export const QuizAttemptStatus = t.Union(
  [t.Literal("IN_PROGRESS"), t.Literal("COMPLETED"), t.Literal("ABANDONED")],
  { additionalProperties: false },
);
