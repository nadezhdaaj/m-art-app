import { t } from "elysia";
import { __nullable__ } from "./__nullable__";
import { __transformDate__ } from "./__transformDate__";

export const NewsPostSourceType = t.Union([t.Literal("MANUAL"), t.Literal("IMPORT")], {
  additionalProperties: false,
});
