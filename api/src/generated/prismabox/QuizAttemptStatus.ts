import { t } from "elysia";

import { __transformDate__ } from "./__transformDate__";

import { __nullable__ } from "./__nullable__";

export const QuizAttemptStatus = t.Union(
  [t.Literal("IN_PROGRESS"), t.Literal("COMPLETED"), t.Literal("ABANDONED")],
  { additionalProperties: false },
);
