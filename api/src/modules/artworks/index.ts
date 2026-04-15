import { Elysia } from "elysia";
import { ErrorDto } from "@/lib/http";
import { protectedPlugin } from "../auth";
import { ArtworkModels } from "./model";
import { createArtwork, getArtwork, getArtworks, updateArtwork } from "./services";

export const artworksPlugin = new Elysia({ prefix: "/artworks", detail: { tags: ["Artworks"] } })
  .use(protectedPlugin)
  .get("/me", ({ user }) => getArtworks(user.id), {
    response: {
      200: ArtworkModels.ArtworkList,
      404: ErrorDto,
    },
    detail: {
      summary: "List my saved artworks",
    },
  })
  .get("/me/:artworkId", ({ user, params }) => getArtwork(user.id, params.artworkId), {
    response: {
      200: ArtworkModels.Artwork,
      404: ErrorDto,
    },
    detail: {
      summary: "Get my saved artwork",
    },
  })
  .post("/me", ({ user, body }) => createArtwork(user.id, body), {
    body: ArtworkModels.CreateArtworkBody,
    response: {
      200: ArtworkModels.Artwork,
      400: ErrorDto,
      404: ErrorDto,
    },
    detail: {
      summary: "Save a new artwork",
    },
  })
  .patch("/me/:artworkId", ({ user, params, body }) => updateArtwork(user.id, params.artworkId, body), {
    body: ArtworkModels.UpdateArtworkBody,
    response: {
      200: ArtworkModels.Artwork,
      400: ErrorDto,
      404: ErrorDto,
    },
    detail: {
      summary: "Update saved artwork metadata or files",
    },
  });
