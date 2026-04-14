# Проектирование: результаты викторин, опыт и достижения

## Цель

Поддержать завершение викторины как доменное событие, после которого backend:

- сохраняет итог попытки;
- начисляет опыт;
- вычисляет уровень пользователя из XP;
- выдает достижения один раз;
- возвращает Unity-клиенту готовый результат прохождения вместе с наградами.

Документ опирается на стек `Elysia + Bun + Prisma v7 + PostgreSQL`.

## Основная идея

Разделить систему на 3 слоя:

1. `quiz` отвечает за игровой процесс и формирование результата попытки.
2. `progress` отвечает за XP, уровень и историю событий прогресса.
3. `rewards` отвечает за вычисление и выдачу наград по доменным событиям.

Ключевое правило: модуль викторин не должен сам вручную начислять XP в нескольких местах. Он должен завершать попытку и передавать это в единый `RewardService`.

## Предлагаемая модель данных

### 1. Оставить `Profile` минимальным

В `Profile` достаточно хранить только базовые данные пользователя и `xp`.

- `xp Int`

Остальные значения лучше не денормализовать:

- `level` вычисляется из `xp`;
- `totalQuizCompletions` считается по `quiz_attempt`;
- `totalQuizCorrectAnswers` считается по завершенным попыткам;
- `lastRewardedAt` определяется по последнему `ProgressEvent`.

Так модель остается проще и не требует синхронизации агрегатов в нескольких местах.

### 2. Добавить `QuizAttempt`

`QuizAttempt` должна быть единой сущностью и для текущего состояния сессии, и для завершенной попытки.

### 3. Добавить `QuizAttemptAnswer`

Нужна для истории ответов, аналитики и прозрачной выдачи результата.

### 4. Добавить `ProgressEvent`

Это журнал всех начислений и изменений прогресса. Он решает идемпотентность и позволяет объяснить, откуда взялся XP.

### 5. Расширить `Achievement`

Минимально:

- `category`
- `rarity`
- `ruleType`
- `ruleConfig`
- `xpReward`

## Почему лучше не вводить отдельный `UserQuizProgress`

Отдельная таблица текущего прогресса дублирует состояние, которое можно хранить прямо в `QuizAttempt`:

- порядок вопросов;
- отвеченные вопросы;
- число верных ответов;
- текущий шаг прохождения;
- момент завершения.

## Как считать XP

Формулу лучше держать вне роутов.

Пример базовой схемы:

- завершение квиза: `+50 XP`
- за каждый правильный ответ: `+10 XP`
- за 100% точность: `+25 XP`
- за первое прохождение конкретного квиза: `+20 XP`

## Как считать уровень

Использовать helper:

```ts
export const getLevelByXp = (xp: number) => { ... }
export const getLevelProgress = (xp: number) => { ... }
```

## DTO результата

### RewardSummary

```ts
const RewardSummary = t.Object({
  gainedXp: t.Integer(),
  totalXp: t.Integer(),
  previousLevel: t.Integer(),
  currentLevel: t.Integer(),
  leveledUp: t.Boolean(),
  unlockedAchievements: t.Array(...),
});
```

### QuizCompletionResult

```ts
const QuizCompletionResult = t.Object({
  attemptId: t.String(),
  quizId: t.String(),
  score: t.Integer(),
  totalQuestions: t.Integer(),
  correctAnswers: t.Integer(),
  wrongAnswers: t.Integer(),
  accuracyPercent: t.Integer(),
  completedAt: t.String({ format: "date-time" }),
  rewards: RewardSummary,
});
```

## Нужные endpoint'ы

### В `quiz`

- `GET /quizzes/:id/results/:attemptId`

### В `progress`

- `GET /progress`
- `GET /progress/history`
- `GET /achievements`

## Итоговая рекомендация

Лучший путь для текущего проекта:

- оставить `quiz` модулю игровой flow;
- добавить отдельный reward pipeline;
- ввести отдельную сущность результата попытки;
- возвращать награды сразу в результате завершения викторины;
- не хранить производные агрегаты уровня и статистики викторин в `Profile`, если их можно надежно вывести из `xp`, `quiz_attempt` и `progress_event`.
