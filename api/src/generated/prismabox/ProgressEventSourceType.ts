import { t } from "elysia";
import { __nullable__ } from "./__nullable__";
import { __transformDate__ } from "./__transformDate__";

export const ProgressEventSourceType = t.Union(
  [
    t.Literal("QUIZ_ATTEMPT"),
    t.Literal("EXHIBIT_VIEW"),
    t.Literal("DAILY_LOGIN"),
    t.Literal("ARTWORK_CREATED"),
  ],
  { additionalProperties: false },
);
