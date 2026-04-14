import { t } from "elysia";

export const ErrorDto = t.Object({
  code: t.String(),
  message: t.String(),
  details: t.Optional(t.Unknown()),
});
