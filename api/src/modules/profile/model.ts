import { t, UnwrapSchema } from "elysia";
import { ProfilePlain } from "@/generated/prismabox/Profile";
import { ProgressModels } from "../progress/model";

export const Profile = ProfilePlain;
export type Profile = UnwrapSchema<typeof Profile>;

export const UpdateProfileBody = t.Object(
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
);
export type UpdateProfileBody = UnwrapSchema<typeof UpdateProfileBody>;

export const ProfileOverview = t.Object({
  profile: ProfilePlain,
  progression: ProgressModels.ProgressResponse,
});
export type ProfileOverview = UnwrapSchema<typeof ProfileOverview>;

export const ProfileModels = {
  Profile,
  UpdateProfileBody,
  ProfileOverview,
} as const;
