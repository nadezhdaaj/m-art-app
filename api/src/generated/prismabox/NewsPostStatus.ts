import { t } from "elysia";

import { __transformDate__ } from "./__transformDate__";

import { __nullable__ } from "./__nullable__";

export const NewsPostStatus = t.Union(
  [t.Literal("DRAFT"), t.Literal("PUBLISHED"), t.Literal("ARCHIVED")],
  { additionalProperties: false },
);
