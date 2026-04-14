export const getXpRequiredForLevel = (level: number) => {
  if (level <= 1) {
    return 0;
  }

  return 25 * (level - 1) * (level + 2);
};

export const getLevelByXp = (xp: number) => {
  let level = 1;

  while (getXpRequiredForLevel(level + 1) <= xp) {
    level += 1;
  }

  return level;
};

export const getLevelProgress = (xp: number) => {
  const level = getLevelByXp(xp);
  const currentLevelXp = getXpRequiredForLevel(level);
  const nextLevelXp = getXpRequiredForLevel(level + 1);

  return {
    level,
    currentLevelXp,
    nextLevelXp,
    xpIntoLevel: xp - currentLevelXp,
    xpToNextLevel: nextLevelXp - xp,
  };
};
