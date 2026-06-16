const path = require("path");
require("dotenv").config({ path: path.join(__dirname, "..", ".env") });

const { PrismaClient } = require("@prisma/client");
const { MINI_GAME_SCREEN_1 } = require("./quiz-questions-bank");

const prisma = new PrismaClient();

async function main() {
  const mismatches = [];

  for (const item of MINI_GAME_SCREEN_1) {
    const question = item.question.trim();
    const bankTexts = item.answers.map((t) => t.trim());

    if (item.correctIndex < 0 || item.correctIndex > 3) {
      mismatches.push({ question, issue: "correctIndex out of range", correctIndex: item.correctIndex });
    }

    if (bankTexts.length !== 4) {
      mismatches.push({ question, issue: "not 4 answers", count: bankTexts.length });
    }

    const db = await prisma.quizQuestion.findUnique({
      where: { question },
      include: { answers: { orderBy: { sortOrder: "asc" } } },
    });

    if (!db) {
      mismatches.push({ question, issue: "missing in DB" });
      continue;
    }

    const dbTexts = db.answers.map((a) => a.text);

    if (db.correctIndex !== item.correctIndex) {
      mismatches.push({
        question,
        issue: "correctIndex mismatch",
        db: db.correctIndex,
        bank: item.correctIndex,
      });
    }

    if (JSON.stringify(dbTexts) !== JSON.stringify(bankTexts)) {
      mismatches.push({
        question,
        issue: "answer order/text mismatch",
        dbTexts,
        bankTexts,
        dbCorrect: dbTexts[db.correctIndex],
        bankCorrect: bankTexts[item.correctIndex],
      });
    }
  }

  console.log(`Checked ${MINI_GAME_SCREEN_1.length} questions`);
  console.log(`Mismatches: ${mismatches.length}`);
  mismatches.forEach((m) => console.log(JSON.stringify(m, null, 2)));
}

main()
  .then(() => prisma.$disconnect())
  .catch((e) => {
    console.error(e);
    prisma.$disconnect();
    process.exit(1);
  });
