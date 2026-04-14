import { t } from "elysia";

import { __transformDate__ } from "./__transformDate__";

import { __nullable__ } from "./__nullable__";

export const ProgressEventType = t.Union(
  [t.Literal("QUIZ_COMPLETED"), t.Literal("ACHIEVEMENT_UNLOCKED")],
  { additionalProperties: false },
);
