import { s3Storage } from "@/lib/s3";

export const toPublicAvatarUrl = <T extends { avatarUrl: string | null }>(profile: T) => ({
  ...profile,
  avatarUrl: s3Storage.resolvePublicUrl(profile.avatarUrl),
});
