import { status } from "elysia";
import { ERROR_CODES } from "@/lib/http";
import { s3Storage } from "@/lib/s3";

const ARTWORK_STORAGE_PREFIX = "artworks";
const ALLOWED_IMAGE_CONTENT_TYPES = new Set(["image/png", "image/jpeg", "image/webp"]);

const getFileExtension = (contentType: string, fileName: string) => {
  switch (contentType) {
    case "image/png":
      return "png";
    case "image/jpeg":
      return "jpg";
    case "image/webp":
      return "webp";
    default: {
      const fallback = fileName.split(".").pop()?.toLowerCase();
      return fallback || "bin";
    }
  }
};

const assertImageFile = (file: File, fieldName: "image" | "thumbnail") => {
  const contentType = file.type?.trim().toLowerCase();

  if (!contentType || !ALLOWED_IMAGE_CONTENT_TYPES.has(contentType)) {
    throw status(400, {
      code: ERROR_CODES.ARTWORK_IMAGE_INVALID_CONTENT_TYPE,
      message: `Field "${fieldName}" supports only PNG, JPEG, or WebP images.`,
    });
  }

  return contentType;
};

export const uploadArtworkMedia = async ({
  userId,
  artworkId,
  file,
  fieldName,
}: {
  userId: string;
  artworkId: string;
  file: File;
  fieldName: "image" | "thumbnail";
}) => {
  const contentType = assertImageFile(file, fieldName);
  const extension = getFileExtension(contentType, file.name);
  const objectKey = `${ARTWORK_STORAGE_PREFIX}/${userId}/${artworkId}/${fieldName}-${crypto.randomUUID()}.${extension}`;

  const uploaded = await s3Storage.upload({
    key: objectKey,
    body: file,
    contentType,
  });

  return uploaded.key;
};
