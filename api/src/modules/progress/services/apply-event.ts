import type { Prisma, ProgressEventSourceType, ProgressEventType } from "@/generated/prisma/client";

export type ApplyEventInput = {
  tx: Prisma.TransactionClient;
  profileId: string;
  sourceType: ProgressEventSourceType;
  sourceId: string;
  eventType: ProgressEventType;
  xpDelta: number;
  idempotencyKey: string;
  payload?: Prisma.InputJsonValue;
};

export const applyEvent = async (input: ApplyEventInput) => {
  const existingEvent = await input.tx.progressEvent.findUnique({
    where: { idempotencyKey: input.idempotencyKey },
  });

  if (existingEvent) {
    const profile = await input.tx.profile.findUniqueOrThrow({
      where: { id: input.profileId },
    });

    return {
      applied: false,
      event: existingEvent,
      profile,
    };
  }

  const currentProfile = await input.tx.profile.findUniqueOrThrow({
    where: { id: input.profileId },
  });

  const nextXp = currentProfile.xp + input.xpDelta;

  const event = await input.tx.progressEvent.create({
    data: {
      profileId: input.profileId,
      sourceType: input.sourceType,
      sourceId: input.sourceId,
      eventType: input.eventType,
      xpDelta: input.xpDelta,
      payload: input.payload,
      idempotencyKey: input.idempotencyKey,
    },
  });

  const profile = await input.tx.profile.update({
    where: { id: input.profileId },
    data: {
      xp: nextXp,
    },
  });

  return {
    applied: true,
    event,
    profile,
  };
};
