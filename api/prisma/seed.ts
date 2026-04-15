import { PrismaPg } from "@prisma/adapter-pg";
import {
  NewsPostSourceType,
  NewsPostStatus,
  PrismaClient,
  QuizStatus,
  QuizType,
} from "../src/generated/prisma/client";

const databaseUrl = process.env.DATABASE_URL;

if (!databaseUrl) {
  throw new Error("DATABASE_URL is not set");
}

const prisma = new PrismaClient({
  adapter: new PrismaPg({ connectionString: databaseUrl }),
});

const quizSeed = {
  slug: "mart-main-quiz",
  title: "Викторина M'ART",
  description: "Основная музейная викторина, которая раньше была захардкожена в Unity-сцене.",
  previewImageUrl: null,
  type: QuizType.MULTIPLE_CHOICE,
  status: QuizStatus.PUBLISHED,
  imagePool: [] as Array<{ imageUrl: string; sortOrder: number }>,
  questions: [
    {
      text: "Какую художественную технику можно узнать, посещая экспозиции художника Русухина?",
      fact: "Художник использует в современной живописи древнюю технику левкаса — тот же грунт, который применяли мастера русской иконописи.",
      imageUrl: null,
      answers: [
        { orderIndex: 0, text: "Акварель", isCorrect: false },
        { orderIndex: 1, text: "Левкас", isCorrect: true },
        { orderIndex: 2, text: "Граффити", isCorrect: false },
        { orderIndex: 3, text: "Пастель", isCorrect: false },
      ],
    },
    {
      text: "Какой элемент чаще всего используется для передачи динамики циркового представления в живописи?",
      fact: "Художники цирковой темы часто искажают позы и линии, чтобы передать скорость и движение. Такой прием использовал Марк Шагал, изображая летающих акробатов и клоунов.",
      imageUrl: null,
      answers: [
        { orderIndex: 0, text: "Статичные позы", isCorrect: false },
        { orderIndex: 1, text: "Черно-белая гамма", isCorrect: false },
        { orderIndex: 2, text: "Геометрические фигуры", isCorrect: false },
        { orderIndex: 3, text: "Движение и яркие цвета", isCorrect: true },
      ],
    },
    {
      text: "Почему в музеях часто нельзя фотографировать со вспышкой?",
      fact: "Яркий свет может постепенно выцветать пигменты в картинах и старых фотографиях. Поэтому даже в таких музеях, как Лувр и Государственный Эрмитаж, во многих залах запрещают использовать вспышку, чтобы лучше сохранить произведения искусства.",
      imageUrl: null,
      answers: [
        { orderIndex: 0, text: "Свет может повредить краски картин", isCorrect: true },
        { orderIndex: 1, text: "Это мешает охране", isCorrect: false },
        { orderIndex: 2, text: "Камера плохо снимает", isCorrect: false },
        { orderIndex: 3, text: "Фотографии получаются слишком яркими", isCorrect: false },
      ],
    },
    {
      text: "Какое новое пространство для художников создано в музее?",
      fact: "В арт-резиденции художники могут жить и работать прямо в музее, создавая новые проекты. Иногда посетители могут увидеть процесс создания искусства почти вживую.",
      imageUrl: null,
      answers: [
        { orderIndex: 0, text: "Театральная сцена", isCorrect: false },
        { orderIndex: 1, text: "Музыкальная студия", isCorrect: false },
        { orderIndex: 2, text: "Танцевальный зал", isCorrect: false },
        { orderIndex: 3, text: "Арт-резиденция для художников", isCorrect: true },
      ],
    },
    {
      text: "Почему музей M'ART считается уникальным для региона?",
      fact: "Музей M'ART расположен в историческом здании Ордонансгауз, которое после реконструкции стало современным выставочным пространством с множеством залов.",
      imageUrl: null,
      answers: [
        { orderIndex: 0, text: "Это самый старый музей", isCorrect: false },
        { orderIndex: 1, text: "Там нет картин", isCorrect: false },
        {
          orderIndex: 2,
          text: "Это крупнейший и самый современный государственный музей области",
          isCorrect: true,
        },
        { orderIndex: 3, text: "Он работает только летом", isCorrect: false },
      ],
    },
    {
      text: "Какие мероприятия кроме выставок проходят в музее?",
      fact: "В музеях современного искусства можно не только смотреть картины, но и участвовать в мастер-классах и встречаться с художниками, узнавая, как создается искусство.",
      imageUrl: null,
      answers: [
        { orderIndex: 0, text: "Спортивные соревнования", isCorrect: false },
        { orderIndex: 1, text: "Лекции, встречи и мастер-классы", isCorrect: true },
        { orderIndex: 2, text: "Концерты", isCorrect: false },
        { orderIndex: 3, text: "Футбольные матчи", isCorrect: false },
      ],
    },
    {
      text: "Как называется художественный прием, когда краска наносится густыми мазками?",
      fact: "При импасто краска выступает над поверхностью картины и создает рельефную текстуру.",
      imageUrl: null,
      answers: [
        { orderIndex: 0, text: "Импасто", isCorrect: true },
        { orderIndex: 1, text: "Лессировка", isCorrect: false },
        { orderIndex: 2, text: "Фреска", isCorrect: false },
        { orderIndex: 3, text: "Гравюра", isCorrect: false },
      ],
    },
    {
      text: "Как называется произведение искусства, созданное специально для определенного пространства музея?",
      fact: "Такие работы создаются специально под конкретный зал или место.",
      imageUrl: null,
      answers: [
        { orderIndex: 0, text: "Эскиз", isCorrect: false },
        { orderIndex: 1, text: "Портрет", isCorrect: false },
        { orderIndex: 2, text: "Сайт-специфик (site-specific)", isCorrect: true },
        { orderIndex: 3, text: "Репродукция", isCorrect: false },
      ],
    },
    {
      text: "Что такое инсталляция, которую можно увидеть на современных выставках музея?",
      fact: "Инсталляция — это композиция из предметов, света, звука или других элементов, размещенных в пространстве так, чтобы зритель воспринимал работу целиком.",
      imageUrl: null,
      answers: [
        { orderIndex: 0, text: "Картина в рамке", isCorrect: false },
        { orderIndex: 1, text: "Скульптура из камня", isCorrect: false },
        { orderIndex: 2, text: "Гравюра на бумаге", isCorrect: false },
        {
          orderIndex: 3,
          text: "Композиция из предметов, размещенных в пространстве",
          isCorrect: true,
        },
      ],
    },
  ],
};

const achievementSeeds = [
  {
    code: "QUIZ_FIRST_COMPLETION",
    name: "Первый шаг",
    description: "Завершите свою первую викторину.",
    iconUrl: null,
    category: "quiz",
    rarity: "common",
    ruleType: "QUIZ_COMPLETIONS" as const,
    ruleConfig: { minCompletions: 1 },
    xpReward: 25,
    isHidden: false,
  },
  {
    code: "QUIZ_PERFECT_SCORE",
    name: "Без ошибок",
    description: "Получите 100% в любой викторине.",
    iconUrl: null,
    category: "quiz",
    rarity: "rare",
    ruleType: "QUIZ_PERFECT_SCORE" as const,
    ruleConfig: {},
    xpReward: 50,
    isHidden: false,
  },
  {
    code: "QUIZ_MASTER_MAIN",
    name: "Мастер M'ART",
    description: "Пройдите основную викторину M'ART без ошибок.",
    iconUrl: null,
    category: "quiz",
    rarity: "epic",
    ruleType: "QUIZ_SINGLE_COMPLETION" as const,
    ruleConfig: { quizSlug: "mart-main-quiz", minCorrectAnswers: 10 },
    xpReward: 75,
    isHidden: false,
  },
  {
    code: "QUIZ_COMPLETIONS_5",
    name: "Постоянный посетитель",
    description: "Завершите 5 викторин.",
    iconUrl: null,
    category: "quiz",
    rarity: "rare",
    ruleType: "QUIZ_COMPLETIONS" as const,
    ruleConfig: { minCompletions: 5 },
    xpReward: 40,
    isHidden: false,
  },
  {
    code: "QUIZ_CORRECT_ANSWERS_25",
    name: "Знаток коллекции",
    description: "Дайте 25 правильных ответов суммарно.",
    iconUrl: null,
    category: "quiz",
    rarity: "rare",
    ruleType: "QUIZ_CORRECT_ANSWERS" as const,
    ruleConfig: { minCorrectAnswers: 25 },
    xpReward: 40,
    isHidden: false,
  },
];

const newsPostSeeds = [
  {
    slug: "mart-spring-residency-open",
    title: "Весенняя арт-резиденция M'ART открывает новый набор",
    excerpt:
      "Музей объявил набор художников в арт-резиденцию. Участники смогут работать с коллекцией, кураторами и публичной программой музея.",
    coverImageUrl: null,
    content: `Музей M'ART открывает новый сезон арт-резиденции и приглашает художников подать заявки на участие.

В течение программы участники смогут работать с выставочными пространствами музея, исследовать коллекцию и готовить собственные проекты для публичного показа.

На первом этапе новостной раздел заполняется вручную, но структура уже готова для будущего импорта материалов из внешних источников и административной панели.`,
    status: NewsPostStatus.PUBLISHED,
    sourceType: NewsPostSourceType.MANUAL,
    sourceUrl: null,
    sourceExternalId: null,
    publishedAt: new Date("2026-04-01T09:00:00.000Z"),
  },
  {
    slug: "mart-family-weekend-program",
    title: "Семейные выходные в музее: лекции, мастер-классы и экскурсии",
    excerpt:
      "В ближайшие выходные музей проведет семейную программу с интерактивными экскурсиями и творческими занятиями для детей и родителей.",
    coverImageUrl: null,
    content: `В музее M'ART стартует семейная программа выходного дня.

Посетителей ждут экскурсии по экспозиции, короткие лекции о современном искусстве и практические мастер-классы, где можно создать собственную работу по мотивам увиденного.

Новостной модуль хранит полную статью, фотографию и дополнительные поля источника, чтобы в дальнейшем публикации можно было получать автоматически.`,
    status: NewsPostStatus.PUBLISHED,
    sourceType: NewsPostSourceType.MANUAL,
    sourceUrl: null,
    sourceExternalId: null,
    publishedAt: new Date("2026-04-10T12:00:00.000Z"),
  },
];

const main = async () => {
  await prisma.$transaction(async (tx) => {
    const quiz = await tx.quiz.upsert({
      where: { slug: quizSeed.slug },
      create: {
        slug: quizSeed.slug,
        title: quizSeed.title,
        description: quizSeed.description,
        previewImageUrl: quizSeed.previewImageUrl,
        type: quizSeed.type,
        status: quizSeed.status,
      },
      update: {
        title: quizSeed.title,
        description: quizSeed.description,
        previewImageUrl: quizSeed.previewImageUrl,
        type: quizSeed.type,
        status: quizSeed.status,
      },
    });

    await tx.quizImage.deleteMany({
      where: { quizId: quiz.id },
    });

    await tx.quizQuestion.deleteMany({
      where: { quizId: quiz.id },
    });

    if (quizSeed.imagePool.length > 0) {
      await tx.quizImage.createMany({
        data: quizSeed.imagePool.map((image) => ({
          quizId: quiz.id,
          imageUrl: image.imageUrl,
          sortOrder: image.sortOrder,
        })),
      });
    }

    for (const question of quizSeed.questions) {
      await tx.quizQuestion.create({
        data: {
          quizId: quiz.id,
          text: question.text,
          fact: question.fact,
          imageUrl: question.imageUrl,
          answers: {
            create: question.answers,
          },
        },
      });
    }

    for (const achievement of achievementSeeds) {
      await tx.achievement.upsert({
        where: { code: achievement.code },
        create: achievement,
        update: {
          name: achievement.name,
          description: achievement.description,
          iconUrl: achievement.iconUrl,
          category: achievement.category,
          rarity: achievement.rarity,
          ruleType: achievement.ruleType,
          ruleConfig: achievement.ruleConfig,
          xpReward: achievement.xpReward,
          isHidden: achievement.isHidden,
        },
      });
    }

    for (const newsPost of newsPostSeeds) {
      await tx.newsPost.upsert({
        where: { slug: newsPost.slug },
        create: newsPost,
        update: {
          title: newsPost.title,
          excerpt: newsPost.excerpt,
          coverImageUrl: newsPost.coverImageUrl,
          content: newsPost.content,
          status: newsPost.status,
          sourceType: newsPost.sourceType,
          sourceUrl: newsPost.sourceUrl,
          sourceExternalId: newsPost.sourceExternalId,
          publishedAt: newsPost.publishedAt,
        },
      });
    }
  });

  console.log(`Seeded quiz, achievements and news posts: ${quizSeed.slug}`);
};

main()
  .catch((error) => {
    console.error("Quiz seed failed");
    console.error(error);
    process.exitCode = 1;
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
