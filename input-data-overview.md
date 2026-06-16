# Входные данные приложения M-ART

Обзор данных, поступающих в систему через REST API бэкенда ([mart-backend/index.js](mart-backend/index.js)).

## Таблица 1 — Аутентификация и регистрация

| Поле | Тип | Эндпоинт | Описание |
|---|---|---|---|
| username | строка | `POST /auth/register` | Имя пользователя при регистрации |
| email | строка | `POST /auth/register`, `/auth/login`, `/auth/sign-up/email`, `/auth/sign-in/email` | Электронная почта (логин) |
| password | строка | `POST /auth/register`, `/auth/login`, `/auth/sign-up/email`, `/auth/sign-in/email` | Пароль |
| bio | строка | `POST /auth/register` | Краткая биография пользователя |
| name | строка | `POST /auth/sign-up/email` | Имя пользователя при регистрации (альтернативный флоу) |

## Таблица 2 — Профиль пользователя

| Поле | Тип | Эндпоинт | Описание |
|---|---|---|---|
| username | строка | `PATCH /profile/me` | Новое имя пользователя |
| bio | строка | `PATCH /profile/me` | Новая биография |
| avatar | файл (multipart) | `PATCH /profile/me`, `PATCH /profile/:id/avatar` | Изображение аватара (сохраняется в `/uploads/avatars/`) |
| points | число | `POST /profile/:id/add-points` | Количество начисляемых очков |
| id | параметр пути | `GET/PATCH /profile/:id`, `PATCH /profile/:id/avatar` | Идентификатор пользователя |

## Таблица 3 — Произведения искусства (Artwork)

| Поле | Тип | Эндпоинт | Описание |
|---|---|---|---|
| title | строка | `POST/PATCH /artworks/me*` | Название произведения |
| description | строка | `POST/PATCH /artworks/me*` | Описание произведения |
| kind | строка (по умолчанию `painting`) | `POST/PATCH /artworks/me*` | Тип произведения |
| source | строка (по умолчанию `paint-canvas`) | `POST/PATCH /artworks/me*` | Источник создания |
| status | строка (по умолчанию `DRAFT`) | `POST/PATCH /artworks/me*` | Статус произведения |
| schemaVersion | число (по умолчанию `1`) | `POST/PATCH /artworks/me*` | Версия схемы данных произведения |
| image | файл (multipart) | `POST/PATCH /artworks/me*` | Изображение произведения |
| thumbnail | файл (multipart) | `POST/PATCH /artworks/me*` | Миниатюра изображения |
| id | параметр пути | `GET/PATCH /artworks/me/:id` | Идентификатор произведения |

## Таблица 4 — Избранное и просмотры экспонатов

| Поле | Тип | Эндпоинт | Описание |
|---|---|---|---|
| exhibitId | параметр пути | `POST/DELETE /favorites/exhibits/:exhibitId` | Идентификатор экспоната для добавления/удаления из избранного |
| exhibitId | параметр пути | `POST /progress/exhibits/:exhibitId/view` | Идентификатор просмотренного экспоната |

## Таблица 5 — Гид (чат-помощник)

| Поле | Тип | Эндпоинт | Описание |
|---|---|---|---|
| topicId | строка | `POST /guide/chat` | Идентификатор темы беседы |
| message | строка | `POST /guide/chat` | Текст сообщения пользователя |

## Таблица 6 — Викторина (Quiz)

| Поле | Тип | Эндпоинт | Описание |
|---|---|---|---|
| question | строка | `POST /quiz/add` | Текст вопроса |
| fact | строка | `POST /quiz/add` | Сопутствующий факт к вопросу |
| correctIndex | число | `POST /quiz/add` | Индекс правильного варианта ответа |
| answers | массив строк | `POST /quiz/add` | Варианты ответов |

## Таблица 7 — Сквозные данные

| Поле | Тип | Где используется | Описание |
|---|---|---|---|
| Authorization (JWT) | заголовок запроса | Все защищённые эндпоинты (`authMiddleware`) | Токен авторизации пользователя |
| id / :id | параметр пути | Множество эндпоинтов | Идентификаторы сущностей (пользователь, произведение и т.д.) в URL |

\* `/artworks/me` (создание, `POST`) и `/artworks/me/:id` (обновление, `PATCH`)
