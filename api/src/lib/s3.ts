import { S3Client } from "bun";

type UploadParams = {
  key: string;
  body:
    | string
    | ArrayBuffer
    | SharedArrayBuffer
    | Uint8Array
    | DataView
    | Blob
    | File
    | Response
    | Request
    | Bun.BunFile;
  contentType?: string;
};

type DownloadMode = "bytes" | "text" | "json";

type GetObjectParams = {
  key: string;
  as?: DownloadMode;
  throwIfMissing?: boolean;
};

export class S3StorageError extends Error {
  constructor(
    message: string,
    readonly code: "S3_OBJECT_NOT_FOUND",
    readonly details?: Record<string, unknown>,
  ) {
    super(message);
    this.name = "S3StorageError";
  }
}

const normalizeKey = (key: string) => {
  const normalized = key.trim().replace(/^\/+|\/+$/g, "");

  if (!normalized) {
    throw new Error("S3 object key must not be empty");
  }

  return normalized;
};

const S3_BUCKET = process.env.S3_BUCKET;
const S3_PRESIGN_EXPIRES_IN_SECONDS = Number(process.env.S3_PRESIGN_EXPIRES_IN_SECONDS) || 900;
const S3_PUBLIC_BASE_URL = process.env.S3_PUBLIC_BASE_URL?.replace(/\/+$/g, "");

const client = new S3Client({
  accessKeyId: process.env.S3_ACCESS_KEY_ID,
  secretAccessKey: process.env.S3_SECRET_ACCESS_KEY,
  bucket: S3_BUCKET,
  endpoint: process.env.S3_ENDPOINT,
  region: process.env.S3_REGION,
});

const readObjectBody = (file: ReturnType<typeof client.file>, mode: DownloadMode) => {
  switch (mode) {
    case "text":
      return file.text();
    case "json":
      return file.json();
    case "bytes":
    default:
      return file.bytes();
  }
};

const getPublicUrl = (key: string) => {
  const normalizedKey = normalizeKey(key);

  if (S3_PUBLIC_BASE_URL) {
    return `${S3_PUBLIC_BASE_URL}/${normalizedKey}`;
  }

  const endpoint = process.env.S3_ENDPOINT?.replace(/\/+$/g, "");

  if (!endpoint || !S3_BUCKET) {
    throw new Error("Unable to build a public URL for S3 object");
  }

  return `${endpoint}/${S3_BUCKET}/${normalizedKey}`;
};

const isAbsoluteUrl = (value: string) => {
  try {
    new URL(value);
    return true;
  } catch {
    return false;
  }
};

export const s3Storage = {
  client,

  getPublicUrl,

  resolvePublicUrl(keyOrUrl: string | null | undefined) {
    if (!keyOrUrl) {
      return keyOrUrl ?? null;
    }

    if (isAbsoluteUrl(keyOrUrl)) {
      return keyOrUrl;
    }

    return getPublicUrl(keyOrUrl);
  },

  async upload({ key, body, contentType }: UploadParams) {
    const normalizedKey = normalizeKey(key);

    await client.write(normalizedKey, body, { type: contentType });

    return {
      key: normalizedKey,
      bucket: S3_BUCKET,
      url: getPublicUrl(normalizedKey),
    };
  },

  async exists(key: string) {
    return client.file(normalizeKey(key)).exists();
  },

  async delete(key: string) {
    const normalizedKey = normalizeKey(key);
    await client.file(normalizedKey).delete();

    return { key: normalizedKey, deleted: true };
  },

  async getObject({ key, as = "bytes", throwIfMissing = true }: GetObjectParams) {
    const normalizedKey = normalizeKey(key);
    const file = client.file(normalizedKey);

    if (!(await file.exists())) {
      if (!throwIfMissing) {
        return null;
      }

      throw new S3StorageError(
        `S3 object "${normalizedKey}" was not found`,
        "S3_OBJECT_NOT_FOUND",
        { key: normalizedKey, bucket: S3_BUCKET },
      );
    }

    return {
      key: normalizedKey,
      body: await readObjectBody(file, as),
      contentType: file.type || null,
      size: file.size,
    };
  },

  presignGet(key: string, expiresInSeconds = S3_PRESIGN_EXPIRES_IN_SECONDS) {
    const normalizedKey = normalizeKey(key);

    return client.presign(normalizedKey, {
      method: "GET",
      expiresIn: expiresInSeconds,
    });
  },

  presignPut(key: string, expiresInSeconds = S3_PRESIGN_EXPIRES_IN_SECONDS) {
    const normalizedKey = normalizeKey(key);

    return client.presign(normalizedKey, {
      method: "PUT",
      expiresIn: expiresInSeconds,
    });
  },
};
