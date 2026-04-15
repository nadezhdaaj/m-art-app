import { t } from "elysia";
import { __nullable__ } from "./__nullable__";
import { __transformDate__ } from "./__transformDate__";

export const ProgressEventType = t.Union(
  [t.Literal("QUIZ_COMPLETED"), t.Literal("ACHIEVEMENT_UNLOCKED")],
  { additionalProperties: false },
);
