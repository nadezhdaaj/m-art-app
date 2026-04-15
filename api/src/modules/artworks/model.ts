import { t, type UnwrapSchema } from "elysia";

const ArtworkStatus = t.Union([
  t.Literal("DRAFT"),
  t.Literal("PUBLISHED"),
  t.Literal("ARCHIVED"),
]);
export type ArtworkStatus = UnwrapSchema<typeof ArtworkStatus>;

const Artwork = t.Object({
  id: t.String(),
  title: t.Nullable(t.String()),
  description: t.Nullable(t.String()),
  kind: t.String(),
  source: t.Nullable(t.String()),
  status: ArtworkStatus,
  schemaVersion: t.Integer(),
  imageUrl: t.String(),
  thumbnailUrl: t.Nullable(t.String()),
  metadata: t.Nullable(t.Any()),
  analysis: t.Nullable(t.Any()),
  publishedAt: t.Nullable(t.String({ format: "date-time" })),
  createdAt: t.String({ format: "date-time" }),
  updatedAt: t.String({ format: "date-time" }),
});
export type Artwork = UnwrapSchema<typeof Artwork>;

const ArtworkList = t.Array(Artwork);
export type ArtworkList = UnwrapSchema<typeof ArtworkList>;

const CreateArtworkBody = t.Object(
  {
    kind: t.String({ minLength: 1, maxLength: 128 }),
    source: t.Optional(t.String({ minLength: 1, maxLength: 128 })),
    title: t.Optional(t.String({ maxLength: 200 })),
    description: t.Optional(t.String({ maxLength: 5000 })),
    status: t.Optional(ArtworkStatus),
    schemaVersion: t.Optional(t.String()),
    metadataJson: t.Optional(t.String()),
    analysisJson: t.Optional(t.String()),
    image: t.File({
      type: "image",
      maxSize: "15m",
    }),
    thumbnail: t.Optional(
      t.File({
        type: "image",
        maxSize: "10m",
      }),
    ),
  },
  { additionalProperties: false },
);
export type CreateArtworkBody = UnwrapSchema<typeof CreateArtworkBody>;

const UpdateArtworkBody = t.Object(
  {
    kind: t.Optional(t.String({ minLength: 1, maxLength: 128 })),
    source: t.Optional(t.Nullable(t.String({ minLength: 1, maxLength: 128 }))),
    title: t.Optional(t.Nullable(t.String({ maxLength: 200 }))),
    description: t.Optional(t.Nullable(t.String({ maxLength: 5000 }))),
    status: t.Optional(ArtworkStatus),
    schemaVersion: t.Optional(t.String()),
    metadataJson: t.Optional(t.String()),
    analysisJson: t.Optional(t.String()),
    image: t.Optional(
      t.File({
        type: "image",
        maxSize: "15m",
      }),
    ),
    thumbnail: t.Optional(
      t.File({
        type: "image",
        maxSize: "10m",
      }),
    ),
  },
  { additionalProperties: false },
);
export type UpdateArtworkBody = UnwrapSchema<typeof UpdateArtworkBody>;

export const ArtworkModels = {
  ArtworkStatus,
  Artwork,
  ArtworkList,
  CreateArtworkBody,
  UpdateArtworkBody,
} as const;
