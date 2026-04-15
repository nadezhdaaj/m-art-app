import { t } from "elysia";
import { __nullable__ } from "./__nullable__";
import { __transformDate__ } from "./__transformDate__";

export const QuizType = t.Union([t.Literal("MULTIPLE_CHOICE"), t.Literal("YES_NO")], {
  additionalProperties: false,
});
