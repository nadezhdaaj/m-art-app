import { getProgress } from "@/modules/progress/services";
import { getProfile } from "./get-profile";

export const getOverview = async (userId: string) => {
  const [profile, progression] = await Promise.all([
    getProfile(userId),
    getProgress(userId),
  ]);

  return {
    profile,
    progression,
  };
};
