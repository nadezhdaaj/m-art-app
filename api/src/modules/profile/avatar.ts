import { status } from "elysia";
import { ERROR_CODES } from "@/lib/http";
import { s3Storage } from "@/lib/s3";

const AVATAR_STORAGE_PREFIX = "avatars";
const DEFAULT_AVATAR_CONTENT_TYPE = "application/octet-stream";

const getAvatarFileExtension = (contentType: string, fileName?: string | null) => {
  const normalizedType = contentType.toLowerCase();

  if (normalizedType === "image/jpeg") return "jpg";
  if (normalizedType === "image/png") return "png";
  if (normalizedType === "image/webp") return "webp";
  if (normalizedType === "image/gif") return "gif";
  if (normalizedType === "image/svg+xml") return "svg";
  if (normalizedType === "image/avif") return "avif";

  const rawExtension = fileName?.trim().split(".").pop()?.toLowerCase();

  if (rawExtension && rawExtension.length <= 5) {
    return rawExtension;
  }

  return "bin";
};

export const uploadAvatar = async (userId: string, avatar: File) => {
  const contentType = avatar.type?.trim();

  if (!contentType?.startsWith("image/")) {
    throw status(400, {
      code: ERROR_CODES.PROFILE_AVATAR_INVALID_CONTENT_TYPE,
      message: "Avatar must be an image file.",
      details: {
        fileName: avatar.name,
        contentType: contentType || null,
      },
    });
  }

  const objectKey = `${AVATAR_STORAGE_PREFIX}/${userId}/${crypto.randomUUID()}.${getAvatarFileExtension(contentType, avatar.name)}`;
  const uploadedAvatar = await s3Storage.upload({
    key: objectKey,
    body: avatar,
    contentType: contentType || DEFAULT_AVATAR_CONTENT_TYPE,
  });

  return uploadedAvatar.url;
};
