# Отчет по реализации: результаты викторин, опыт и достижения

## Что было сделано

В рамках первого этапа реализации была внедрена серверная основа для:

- сохранения результата прохождения викторины;
- начисления опыта за завершение викторины;
- расчета уровня пользователя;
- выдачи достижений по правилам;
- возврата итогового `completionResult` сразу после ответа на последний вопрос;
- отдельного получения результата завершенной попытки;
- чтения агрегированного прогресса пользователя.

## Изменения в модели данных

### `Profile`

В итоговой реализации `Profile` оставлен минимальным и хранит:

- данные отображения пользователя;
- `xp` как базовый агрегат прогресса.

Поля `level`, `totalQuizCompletions`, `totalQuizCorrectAnswers` и `lastRewardedAt` были убраны как избыточная денормализация. Теперь:

- уровень вычисляется из `xp`;
- число завершений считается по `quiz_attempt`;
- число правильных ответов считается по завершенным попыткам;
- время последней награды определяется по `progress_event`.

### `QuizAttemptAnswer`

Добавлена таблица истории ответов пользователя:

- `attemptId`
- `questionId`
- `selectedAnswerId`
- `isCorrect`
- `answeredAt`

### `ProgressEvent`

Добавлен журнал событий прогресса:

- `sourceType`
- `sourceId`
- `eventType`
- `xpDelta`
- `payload`
- `idempotencyKey`

Именно он стал единым источником истины для начислений XP и повторных вызовов.

### `Achievement`

Для достижений добавлены машинно-обрабатываемые правила:

- `category`
- `rarity`
- `ruleType`
- `ruleConfig`
- `xpReward`

### Миграция

Создана миграция:

- [prisma/migrations/20260415093000_quiz_results_progress_rewards/migration.sql](/c:/Users/Champ/Desktop/Projects/MART/elysia-app/prisma/migrations/20260415093000_quiz_results_progress_rewards/migration.sql:1)

## Новые модули и сервисы

### `progress`

Добавлен модуль прогресса:

- [src/modules/progress/level.ts](/c:/Users/Champ/Desktop/Projects/MART/elysia-app/src/modules/progress/level.ts:1)
- [src/modules/progress/model.ts](/c:/Users/Champ/Desktop/Projects/MART/elysia-app/src/modules/progress/model.ts:1)
- [src/modules/progress/service.ts](/c:/Users/Champ/Desktop/Projects/MART/elysia-app/src/modules/progress/service.ts:1)
- [src/modules/progress/index.ts](/c:/Users/Champ/Desktop/Projects/MART/elysia-app/src/modules/progress/index.ts:1)

Реализовано:

- вычисление уровня по XP;
- вычисление прогресса внутри уровня;
- применение `ProgressEvent` с `idempotencyKey`;
- обновление XP пользователя;
- чтение агрегатов по викторинам через запросы;
- API получения прогресса;
- API получения разблокированных достижений.

### `rewards`

Добавлен слой наград:

- [src/modules/rewards/rules/quiz.ts](/c:/Users/Champ/Desktop/Projects/MART/elysia-app/src/modules/rewards/rules/quiz.ts:1)
- [src/modules/rewards/achievement.service.ts](/c:/Users/Champ/Desktop/Projects/MART/elysia-app/src/modules/rewards/achievement.service.ts:1)
- [src/modules/rewards/reward.service.ts](/c:/Users/Champ/Desktop/Projects/MART/elysia-app/src/modules/rewards/reward.service.ts:1)

Реализовано:

- расчет XP за завершение викторины;
- бонус за правильные ответы;
- бонус за идеальное прохождение;
- бонус за первое завершение конкретной викторины;
- проверка правил достижений;
- one-time unlock достижений через уникальные ключи и `upsert`;
- начисление XP за достижение через `ProgressEvent`.

## Изменения в модуле викторин

Основные изменения внесены в:

- [src/modules/quiz/service.ts](/c:/Users/Champ/Desktop/Projects/MART/elysia-app/src/modules/quiz/service.ts:1)
- [src/modules/quiz/model.ts](/c:/Users/Champ/Desktop/Projects/MART/elysia-app/src/modules/quiz/model.ts:1)
- [src/modules/quiz/result.mapper.ts](/c:/Users/Champ/Desktop/Projects/MART/elysia-app/src/modules/quiz/result.mapper.ts:1)
- [src/modules/quiz/index.ts](/c:/Users/Champ/Desktop/Projects/MART/elysia-app/src/modules/quiz/index.ts:1)

Что изменилось:

- при каждом ответе создается запись `QuizAttemptAnswer`;
- при ответе на последний вопрос попытка переводится в `COMPLETED`;
- после завершения вызывается `RewardService.grantQuizCompletionRewards(...)`;
- в `POST /quizzes/:quizId/questions/:questionId/answer` добавлено поле `completionResult`;
- добавлен `GET /quizzes/:quizId/results/:attemptId`.

## Новые API

### Progress API

- `GET /progress`
- `GET /progress/achievements`

### Quiz API

- `GET /quizzes/:quizId/results/:attemptId`

## Сиды

В `prisma/seed.ts` добавлены базовые достижения:

- `QUIZ_FIRST_COMPLETION`
- `QUIZ_PERFECT_SCORE`
- `QUIZ_MASTER_MAIN`
- `QUIZ_COMPLETIONS_5`
- `QUIZ_CORRECT_ANSWERS_25`

## Что было проверено

После внесения изменений выполнены:

- `bunx prisma generate`
- `bunx tsc --noEmit`
- `bun run fmt`

## Что пока не сделано

- миграция не применена к реальной БД;
- после обновления сидов не запускался `prisma db seed`;
- не добавлены unit/integration тесты;
- не добавлен `GET /progress/history`;
- в `GET /quizzes/:quizId/results/:attemptId` `totalXp` пока определяется из текущего профиля, а не как исторический снимок на момент завершения попытки.

## Следующие шаги

1. Применить миграцию к базе данных.
2. Выполнить сиды с новыми достижениями.
3. Добавить тесты на расчет XP, уровней и идемпотентность.
4. Доработать историческое восстановление `RewardSummary`.
5. Поддерживать документацию схемы синхронно с `schema.prisma`.
