import { t, UnwrapSchema } from "elysia";
import { ProfilePlain } from "@/generated/prismabox/Profile";

export const profileModels = {
  profile: ProfilePlain,
  updateProfileBody: t.Object(
    {
      displayName: t.Optional(t.Nullable(t.String())),
      avatar: t.Optional(
        t.File({
          type: "image",
          maxSize: "5m",
        }),
      ),
    },
    { additionalProperties: false },
  ),
} as const;

export const Profile = profileModels.profile;
export type Profile = UnwrapSchema<typeof Profile>;

export const UpdateProfileBody = profileModels.updateProfileBody;
export type UpdateProfileBody = UnwrapSchema<typeof UpdateProfileBody>;

export type ProfileModels = {
  [k in keyof typeof profileModels]: UnwrapSchema<(typeof profileModels)[k]>;
};
