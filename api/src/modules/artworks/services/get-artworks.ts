import { prisma } from "@/lib/db";
import { getProfile } from "@/modules/profile/services";
import { mapArtwork } from "../mapper";

export const getArtworks = async (userId: string) => {
  const profile = await getProfile(userId);
  const artworks = await prisma.artwork.findMany({
    where: { profileId: profile.id },
    orderBy: { createdAt: "desc" },
  });

  return artworks.map(mapArtwork);
};
