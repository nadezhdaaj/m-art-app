import { status } from "elysia";
import type { Prisma } from "@/generated/prisma/client";
import { ERROR_CODES } from "@/lib/http";

export const parseOptionalJson = (
  fieldName: "metadataJson" | "analysisJson",
  value: string | undefined,
): Prisma.InputJsonValue | undefined => {
  if (value === undefined) {
    return undefined;
  }

  try {
    return JSON.parse(value) as Prisma.InputJsonValue;
  } catch {
    throw status(400, {
      code: ERROR_CODES.ARTWORK_INVALID_JSON,
      message: `Field "${fieldName}" must contain valid JSON.`,
    });
  }
};

export const parseOptionalInteger = (fieldName: string, value: string | undefined) => {
  if (value === undefined) {
    return undefined;
  }

  const parsed = Number.parseInt(value, 10);
  if (!Number.isInteger(parsed) || parsed <= 0) {
    throw status(400, {
      code: ERROR_CODES.VALIDATION_FAILED,
      message: `Field "${fieldName}" must be a positive integer.`,
    });
  }

  return parsed;
};
