import { t } from "elysia";
import { __nullable__ } from "./__nullable__";
import { __transformDate__ } from "./__transformDate__";

export const QuizStatus = t.Union(
  [t.Literal("DRAFT"), t.Literal("PUBLISHED"), t.Literal("ARCHIVED")],
  { additionalProperties: false },
);
