import { t } from "elysia";

import { __transformDate__ } from "./__transformDate__";

import { __nullable__ } from "./__nullable__";

export const NewsPostSourceType = t.Union(
  [t.Literal("MANUAL"), t.Literal("IMPORT")],
  { additionalProperties: false },
);
