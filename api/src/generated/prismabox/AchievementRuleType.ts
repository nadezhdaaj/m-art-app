import { t } from "elysia";
import { __nullable__ } from "./__nullable__";
import { __transformDate__ } from "./__transformDate__";

export const AchievementRuleType = t.Union(
  [
    t.Literal("QUIZ_COMPLETIONS"),
    t.Literal("QUIZ_PERFECT_SCORE"),
    t.Literal("QUIZ_CORRECT_ANSWERS"),
    t.Literal("QUIZ_SINGLE_COMPLETION"),
  ],
  { additionalProperties: false },
);
