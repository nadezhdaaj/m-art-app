const path = require("path");
require("dotenv").config({ path: path.join(__dirname, "..", ".env") });

const { PrismaClient } = require("@prisma/client");
const { MINI_GAME_SCREEN_1 } = require("./quiz-questions-bank");

const prisma = new PrismaClient();

async function main() {
  let created = 0;
  let updated = 0;

  for (const item of MINI_GAME_SCREEN_1) {
    const q = item.question.trim();
    const answersCreate = item.answers.map((text, index) => ({
      text: text.trim(),
      sortOrder: index,
    }));
    const fact = item.fact.trim();

    const existing = await prisma.quizQuestion.findUnique({
      where: { question: q },
    });

    if (existing) {
      await prisma.quizQuestion.update({
        where: { id: existing.id },
        data: {
          fact,
          correctIndex: item.correctIndex,
          answers: { deleteMany: {}, create: answersCreate },
        },
      });
      updated += 1;
      continue;
    }

    await prisma.quizQuestion.create({
      data: {
        question: q,
        fact,
        correctIndex: item.correctIndex,
        answers: { create: answersCreate },
      },
    });
    created += 1;
  }

  console.log(
    `Quiz «Mini game screen 1»: создано ${created}, обновлено ${updated}, всего в банке ${MINI_GAME_SCREEN_1.length}.`
  );
}

main()
  .then(() => prisma.$disconnect())
  .catch((e) => {
    console.error(e);
    prisma.$disconnect();
    process.exit(1);
  });
