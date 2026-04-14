# M'ART Museum of Modern Art

Полная документация по Unity-клиенту и связанному backend внутри проекта `MART`.

Этот README нужен как карта проекта: куда смотреть в первую очередь, какие папки за что отвечают, как связаны Unity и backend, где лежит авторизация, квизы, AR и профиль, и что сейчас уже реализовано.

## 1. Что это за проект

`M'ART` - мобильное приложение музея современного искусства для детской и молодежной аудитории.

Сейчас проект состоит из двух главных частей:

- `unity/` - клиентское приложение на Unity;
- `elysia-app/` - кастомный backend на `Elysia + Better Auth + Prisma + PostgreSQL`.

Клиент и backend уже связаны между собой:

- Unity использует backend для входа, регистрации, восстановления сессии и загрузки профиля;
- backend хранит пользователей, профили, прогресс, квизы, достижения и сессии;
- Firebase из auth-flow удален, и авторизация больше не зависит от Firebase SDK.

## 2. Структура репозитория

Корень проекта:

- `unity/` - Unity-проект приложения.
- `elysia-app/` - backend API и база данных.
- `FUNCTIONS.md` - общие заметки и вспомогательная документация.
- `PROJECT_CHECKLIST.md` - список проектных задач верхнего уровня.
- `UNITY_AUTH_MIGRATION_PLAN.md` - план миграции Unity-авторизации с Firebase на backend.
- `UNITY_AUTH_MIGRATION_CHECKLIST.md` - практический чеклист этой миграции.

Если ты входишь в проект с нуля, почти вся ежедневная работа будет идти либо в `unity/`, либо в `elysia-app/`.

## 3. Архитектура системы

Система устроена так:

1. Пользователь открывает Unity-приложение.
2. На auth-сцене Unity проверяет, есть ли сохраненный bearer token.
3. Если токен есть, Unity запрашивает `GET /auth/get-session`, затем `GET /profile/me`.
4. Если токен валиден, пользователь попадает в приложение без повторного логина.
5. Если токен невалиден или отсутствует, Unity показывает welcome/login/register flow.
6. Backend отвечает за:
   - создание аккаунта;
   - вход по email и password;
   - хранение сессий;
   - выдачу профиля;
   - обновление профиля;
   - прогресс, квизы и достижения.

Ключевая идея: Unity теперь тонкий клиент, а источник истины по пользователю и прогрессу находится в backend и базе данных.

## 4. Unity-проект: обзор

Папка: `unity/`

Это полноценный Unity-проект. Основные интересующие директории внутри `Assets/`:

- `Assets/Scenes` - сцены приложения.
- `Assets/Scripts` - вся игровая и UI-логика.
- `Assets/Editor` - editor-утилиты и генераторы сцен.
- `Assets/Prefabs` - префабы.
- `Assets/UI` - UI-ассеты.
- `Assets/Fonts` - используемые шрифты.
- `Assets/Models` - 3D-модели и визуальные ассеты.
- `Assets/Materials` - материалы.
- `Assets/StreamingAssets` - runtime-данные Unity.
- `Assets/Plugins` - Android/plugin templates и platform integrations.
- `Assets/ExternalDependencyManager` - Google External Dependency Manager для Unity.
- `Assets/TextMesh Pro` - TextMesh Pro ресурсы.
- `Assets/XR` - AR/XR-настройки.
- `Assets/Settings` - project asset settings.

### 4.1 `Assets/Scenes`

Главные сцены проекта:

- `Registration.unity` - новая auth-сцена под backend.
- `Auth Scene.unity` - старая auth-сцена или промежуточный legacy-экран.
- `SplashLoading.unity` - стартовый загрузочный экран.
- `The main stage.unity` - основной экран приложения.
- `ARScene.unity` - сцена AR.

Если ты работаешь с авторизацией, начни с `Registration.unity`.

### 4.2 `Assets/Scripts`

Папка разбита по подсистемам:

- `Assets/Scripts/Backend`
- `Assets/Scripts/AR`
- `Assets/Scripts/Quiz`
- `Assets/Scripts/Painter`
- `Assets/Scripts/MiniGames`
- `Assets/Scripts/Mini game 2`
- `Assets/Scripts/System`
- `Assets/Scripts/UI`

Ниже разбор каждой папки.

## 5. Unity: папка `Assets/Scripts/Backend`

Это главный слой клиентской бизнес-логики для авторизации и профиля.

Файлы:

- `BackendManager.cs`
- `AuthUIManager.cs`
- `BackendAuthApiClient.cs`
- `BackendAuthModels.cs`
- `SessionStorage.cs`
- `LobbyManager.cs`
- `GameManager.cs`

### 5.1 `BackendManager.cs`

Главный runtime-менеджер backend-авторизации.

Отвечает за:

- хранение singleton-инстанса;
- адрес backend (`Backend Base Url`);
- ссылки на поля логина и регистрации;
- `LoginButton()` и `RegisterButton()`;
- восстановление сессии при старте;
- загрузку профиля после успешной авторизации;
- выход из аккаунта;
- сброс локальной сессии;
- переход из auth-flow в основное приложение.

Что важно знать:

- токен хранится локально;
- проверка сохраненной сессии запускается в `Start()`;
- если токен невалиден, менеджер очищает сессию и возвращает пользователя на auth-экран;
- часть старых методов профиля пока оставлена как заглушка на период миграции.

### 5.2 `AuthUIManager.cs`

UI-переключатель auth-сцены.

Отвечает только за показ и скрытие экранов:

- welcome screen;
- checking screen;
- login screen;
- register screen;
- status screen.

Это не network-layer и не auth logic, а именно менеджер видимости панелей.

### 5.3 `BackendAuthApiClient.cs`

Тонкий HTTP-клиент Unity для общения с backend.

Содержит запросы к endpoint’ам:

- `sign-in`
- `sign-up`
- `get-session`
- `profile/me`
- `sign-out`

Именно этот класс инкапсулирует `UnityWebRequest`, сериализацию JSON и передачу bearer token.

### 5.4 `BackendAuthModels.cs`

DTO-модели запросов и ответов.

Тут лежат:

- модели логина;
- модели регистрации;
- модель пользователя;
- модель профиля;
- модель сессии;
- общие response wrappers.

Если меняется контракт backend auth/profile API, почти всегда правки начинаются отсюда.

### 5.5 `SessionStorage.cs`

Локальное хранилище токена через `PlayerPrefs`.

Назначение:

- сохранить токен после входа;
- восстановить токен после перезапуска;
- очистить токен при sign out или принудительном reset.

Это самый простой ответ на вопрос “где лежит текущая сессия”.

### 5.6 `LobbyManager.cs`

Менеджер профиля на главном экране.

Использует `BackendManager.CurrentUser` и `BackendManager.CurrentProfile`, чтобы:

- показать имя;
- показать email;
- показать avatar URL;
- показать XP и level;
- выполнить sign out.

То есть auth-сцена отвечает за вход, а `LobbyManager` - за отображение уже загруженного профиля после входа.

### 5.7 `GameManager.cs`

Вспомогательный singleton/системный менеджер для переходов и общей игровой логики. Это не backend-класс по смыслу, но сейчас он лежит рядом с auth-слоем исторически.

## 6. Unity: папка `Assets/Scripts/AR`

Это AR-подсистема проекта.

Файлы:

- `OpenAR.cs`
- `ARImageSpawner.cs`
- `ARInteractionController.cs`
- `Navigation.cs`
- `PlaceObject.cs`

### Что здесь реализовано

- переход в AR-сцену;
- отслеживание reference images через `ARTrackedImageManager`;
- спавн префаба поверх распознанного изображения;
- базовая логика взаимодействия и навигации в AR.

### Ключевые файлы

`OpenAR.cs`

- простой переход в сцену `ARScene`.

`ARImageSpawner.cs`

- слушает событие `trackedImagesChanged`;
- создаёт объект на tracked image;
- скрывает или удаляет объекты при потере трекинга.

Если задача касается “почему AR-объект не появляется” или “как подменить prefab над маркером”, смотреть нужно сначала сюда.

## 7. Unity: папка `Assets/Scripts/Quiz`

Файл:

- `QuizManager.cs`

Это локальная quiz/gameplay-логика на стороне Unity.

Что умеет:

- хранить список вопросов;
- показывать текущий вопрос и ответы;
- проверять правильность ответа;
- показывать fact panel;
- считать прогресс по шагам;
- открывать части пазла при правильных ответах;
- завершать игру и показывать end screen;
- перезапускать сцену.

Важно:

- текущий `QuizManager` ориентирован на локальные вопросы, а не на backend quiz API;
- backend quiz module уже существует, но Unity-клиент ещё не переведен на него полностью.

То есть сейчас есть два слоя:

- готовый backend для квизов;
- локальная Unity-реализация quiz scene.

Это одна из следующих зон для интеграции.

## 8. Unity: папка `Assets/Scripts/Painter`

Подсистема рисования и творчества.

Файлы:

- `Painter.cs`
- `PaletteManager.cs`
- `PaletteToggle.cs`
- `BrushSelectionPanel.cs`
- `BrushPanelToggle.cs`
- `BrushModeButton.cs`
- `ColorButton.cs`
- `ImpressionismIntroAndContoursController.cs`
- `PALETTE_SETUP.txt`

### Что делает эта подсистема

- создаёт runtime-texture как холст;
- рисует кистью по `RawImage`;
- поддерживает несколько типов кистей;
- умеет стирать;
- переключает цвета;
- считает метрики мазка;
- управляет палитрой и UI выбора инструмента.

### Ключевой файл: `Painter.cs`

Это ядро всей системы рисования.

Возможности:

- виртуальный холст (`Texture2D`);
- разные типы кистей: `Regular`, `Chalk`, `Marker`, `Pencil`;
- режим ластика;
- очистка холста;
- преобразование координат экрана в координаты текстуры;
- события завершения мазка и сбор статистики по нему.

Если в проекте будут развиваться творческие механики, это одна из самых важных текущих подсистем.

## 9. Unity: папки `Assets/Scripts/MiniGames` и `Assets/Scripts/Mini game 2`

Это локальные игровые механики.

Файлы:

- `MiniGames/LiveContourMiniGame.cs`
- `Mini game 2/SwipeController.cs`

Здесь лежат отдельные мини-игры и вспомогательная механика для них.

Судя по структуре, проект уже экспериментировал с несколькими форматами интерактивных активностей, и эти папки нужно воспринимать как игровой слой поверх музейного контента.

## 10. Unity: папка `Assets/Scripts/System`

Системные и сервисные скрипты.

Файлы:

- `Loader.cs`
- `LoadingController.cs`
- `Required Permissions.cs`

### Назначение

- загрузочные переходы между сценами;
- базовые стартовые задержки;
- platform/runtime permissions.

`Loader.cs` сейчас просто ждёт 2 секунды и загружает `The main stage`.

## 11. Unity: папка `Assets/Scripts/UI`

Файлы:

- `UIController.cs`
- `GamesUI.cs`

Это поверхностный UI-слой для не-auth экранов:

- открытие и скрытие инфо-панелей;
- локальные UI-переключения;
- экранная логика поверх мини-игр и сцен.

`UIController.cs` - пример очень простого контроллера видимости панели.

## 12. Unity: папка `Assets/Editor`

Ключевой файл:

- `RegistrationSceneBuilder.cs`

Это editor-утилита, которая программно пересобирает `Registration.unity` с нуля.

### Зачем это нужно

Auth-сцена содержит много UI-элементов и ссылок между объектами. Вручную поддерживать такой экран неудобно, особенно после миграции с Firebase на backend.

`RegistrationSceneBuilder`:

- создаёт новую пустую сцену;
- добавляет `Camera`, `Canvas`, `EventSystem`;
- создаёт объект `Systems`;
- добавляет `BackendManager` и `AuthUIManager`;
- создаёт welcome/checking/login/register/status экраны;
- автоматически привязывает все serialized-ссылки;
- сохраняет результат в `Assets/Scenes/Registration.unity`.

Если auth-сцена “сломалась”, уехала или ссылки отвалились, не обязательно чинить YAML руками - можно пересобрать сцену этим генератором.

## 13. Backend: обзор

Папка: `elysia-app/`

Это API-сервер на `Bun + Elysia + Better Auth + Prisma + PostgreSQL`.

Главные директории:

- `src/` - исходный код backend.
- `prisma/` - схема БД, миграции, seed.
- `node_modules/` - зависимости.
- `.agents/` - служебные агентные материалы.

Главные файлы:

- `package.json` - scripts и зависимости.
- `docker-compose.yml` - локальная база данных.
- `Dockerfile` - контейнеризация backend.
- `.env` / `.env.example` - переменные окружения.
- `WORK_PLAN.md` - backend-план.
- `BACKEND_CHECKLIST.md` - backend-чеклист.
- `QUIZ_PROGRESS_REWARDS_DESIGN.md` - дизайн системы прогресса и наград.
- `QUIZ_PROGRESS_REWARDS_IMPLEMENTATION.md` - описание реализации этой системы.

## 14. Backend: `package.json` и запуск

Основные команды:

- `bun run dev` - запуск backend в watch-режиме.
- `bun run fmt` - форматирование.
- `bun run seed` - заполнение базы тестовыми данными.

Технологический стек:

- `elysia`
- `better-auth`
- `@prisma/client`
- `prisma`
- `pg`
- `@elysiajs/openapi`

## 15. Backend: `src/index.ts`

Это главный entry point backend.

Он:

- создаёт `Elysia` app;
- подключает OpenAPI;
- подключает плагины модулей:
  - `authPlugin`
  - `profilePlugin`
  - `progressPlugin`
  - `quizPlugin`
- поднимает сервер на `PORT` или `3000`.

То есть все API маршруты входят в приложение именно отсюда.

## 16. Backend: папка `src/lib`

Служебный слой инфраструктуры.

Файлы:

- `db.ts` - инициализация Prisma/db доступа.
- `s3.ts` - работа с S3-совместимым хранилищем.
- `http/error-codes.ts`
- `http/errors.ts`
- `http/index.ts`

Назначение:

- единые коды ошибок;
- типизированные error DTO;
- подключение базы;
- подготовка публичных ссылок на файлы, например аватары.

## 17. Backend: папка `src/modules`

Основная доменная логика backend.

Есть модули:

- `auth`
- `profile`
- `quiz`
- `progress`
- `rewards`

### 17.1 `auth`

Файлы:

- `index.ts`
- `service.ts`
- `complete-register.ts`

Назначение:

- конфигурация Better Auth;
- email/password auth;
- bearer token plugin;
- OpenAPI schema для auth routes;
- post-create hook после регистрации.

`service.ts` настраивает `betterAuth()` с:

- Prisma adapter;
- `basePath: "/auth"`;
- `emailAndPassword.enabled = true`;
- `requireEmailVerification = false`;
- bearer token support.

`complete-register.ts` нужен для постобработки после создания пользователя. Обычно там подготавливаются профиль и связанные данные.

### 17.2 `profile`

Файлы:

- `index.ts`
- `service.ts`
- `model.ts`
- `avatar.ts`

Назначение:

- `GET /profile/me`
- `PATCH /profile/me`
- работа с displayName и avatar;
- генерация публичного URL аватара;
- загрузка файла аватара через storage layer.

Если нужно менять данные профиля, почти вся backend-логика сосредоточена здесь.

### 17.3 `quiz`

Файлы:

- `index.ts`
- `service.ts`
- `model.ts`
- `mapper.ts`
- `result.mapper.ts`
- `utils.ts`

Назначение:

- список опубликованных квизов;
- детальная выдача квиза;
- отправка ответов;
- выдача результатов попытки;
- вычисление порядка вопросов;
- расчёт прогресса и итогов.

Маршруты:

- `GET /quizzes`
- `GET /quizzes/:quizId`
- `GET /quizzes/:quizId/results/:attemptId`
- `POST /quizzes/:quizId/questions/:questionId/answer`

`QuizService` - одно из самых насыщенных мест backend. Там есть:

- получение опубликованного квиза;
- восстановление прогресса пользователя;
- перемешивание вопросов;
- валидация ответов;
- расчёт наград после завершения квиза.

### 17.4 `progress`

Файлы:

- `index.ts`
- `service.ts`
- `model.ts`
- `level.ts`

Назначение:

- агрегированный прогресс пользователя;
- выдача достижений;
- level/xp logic.

Маршруты:

- `GET /progress`
- `GET /progress/achievements`

### 17.5 `rewards`

Файлы:

- `reward.service.ts`
- `achievement.service.ts`
- `rules/quiz.ts`

Назначение:

- начисление XP;
- обработка правил достижений;
- reward summary после квизов;
- привязка достижений к действиям пользователя.

Это модуль “мета-прогрессии” проекта.

## 18. Backend: Prisma и база данных

Папка: `elysia-app/prisma/`

Файлы:

- `schema.prisma`
- `seed.ts`
- `DATABASE_SCHEMA.md`
- `migrations/`

### Основные модели БД

`User`

- базовый пользователь;
- имя, email, image;
- связь с сессиями, аккаунтами и профилем.

`Profile`

- расширение пользователя под продуктовую логику;
- display name;
- avatar URL;
- XP;
- level;
- статистика квизов;
- связи с попытками и достижениями.

`Session`

- bearer-сессии Better Auth;
- токены;
- срок действия;
- связь с пользователем.

`Account`

- auth account record;
- password и provider-данные.

`Verification`

- таблица для verification-related записей Better Auth.

`Quiz`

- квиз;
- slug;
- title;
- description;
- тип;
- статус публикации.

`QuizQuestion`

- вопросы квиза;
- текст;
- fact;
- imageUrl.

`QuizAnswer`

- варианты ответов;
- порядок;
- правильность.

`QuizImage`

- пул изображений квиза.

`QuizAttempt`

- попытка прохождения;
- статус;
- текущий вопрос;
- счёт;
- точность;
- порядок вопросов;
- завершение попытки.

`QuizAttemptAnswer`

- конкретные выбранные ответы пользователя.

`ProgressEvent`

- события прогресса и XP;
- тип события;
- источник;
- idempotency key.

`Achievement`

- справочник достижений;
- правило;
- награда;
- категория;
- редкость.

`UserAchievement`

- связь профиля с полученным достижением;
- время разблокировки;
- прогресс;
- metadata.

## 19. Связь Unity и backend

На текущий момент реализован следующий production-flow:

### Вход

Unity:

- пользователь заполняет email/password;
- `BackendManager` вызывает `BackendAuthApiClient.SignIn(...)`;
- backend возвращает пользователя и bearer token;
- токен сохраняется в `SessionStorage`;
- далее Unity запрашивает профиль.

### Регистрация

Unity:

- пользователь заполняет имя, email, password, confirm password;
- `BackendManager` вызывает backend sign-up;
- после успешной регистрации сохраняется токен;
- Unity грузит профиль и переводит пользователя дальше.

### Восстановление сессии

Unity:

- при старте auth-сцены `BackendManager` читает токен из `PlayerPrefs`;
- вызывает `GET /auth/get-session`;
- потом `GET /profile/me`;
- если всё валидно, делает auto-login.

### Выход

Unity:

- вызывает backend signout;
- очищает локальный токен;
- возвращает пользователя на `Registration`.

### Сброс локальной сессии

Если нужно принудительно удалить текущую сессию:

- на auth-сцене есть кнопка сброса;
- внутри кода это `BackendManager.ClearSavedSessionAndShowLogin()`;
- локально токен хранится в `SessionStorage`.

## 20. Auth-сцена `Registration`

Это новая сцена под backend-flow.

Экранные состояния:

- `Welcome Screen`
- `Checking Screen`
- `Login Screen`
- `Register Screen`
- `Status Screen`

Системные объекты:

- `Canvas`
- `EventSystem`
- `Systems`
- `Screen Root`

На объекте `Systems` находятся:

- `BackendManager`
- `AuthUIManager`

Если в Unity ты не понимаешь, “где логика”, почти всегда сначала нужно выделить в `Hierarchy` объект `Systems`.

## 21. Как открыть и запустить проект локально

### Backend

Из папки `elysia-app/`:

1. Поднять БД:
   `docker compose up -d db`
2. Применить миграции:
   `bunx prisma migrate deploy`
3. Запустить сервер:
   `bun run dev`

По умолчанию backend работает на:

`http://localhost:3000`

### Unity

1. Открыть папку `unity/` в Unity Hub.
2. Открыть сцену `Assets/Scenes/Registration.unity`.
3. Выбрать объект `Systems`.
4. В компоненте `BackendManager` убедиться, что `Backend Base Url = http://localhost:3000`.
5. Нажать `Play`.

## 22. На что смотреть при типовых задачах

### Если сломался логин или регистрация

Смотри:

- `Assets/Scripts/Backend/BackendManager.cs`
- `Assets/Scripts/Backend/BackendAuthApiClient.cs`
- `elysia-app/src/modules/auth`
- `elysia-app/src/modules/profile`

### Если не работает автологин

Смотри:

- `SessionStorage.cs`
- `BackendManager.RestoreSession()`
- backend route `get-session`

### Если не отображается профиль

Смотри:

- `LobbyManager.cs`
- `ProfileService`
- `GET /profile/me`

### Если проблема в auth-сцене или UI-ссылках

Смотри:

- `Registration.unity`
- `RegistrationSceneBuilder.cs`
- `AuthUIManager.cs`

### Если нужно развивать квизы

Смотри:

- Unity: `Assets/Scripts/Quiz/QuizManager.cs`
- backend: `elysia-app/src/modules/quiz`
- rewards: `elysia-app/src/modules/rewards`
- Prisma: `Quiz*`, `ProgressEvent`, `Achievement*`

### Если нужно развивать творчество и рисование

Смотри:

- `Assets/Scripts/Painter/*`

### Если нужно развивать AR

Смотри:

- `Assets/Scripts/AR/*`
- `Assets/Scenes/ARScene.unity`

## 23. Что уже реализовано

На сегодня в проекте уже есть:

- auth flow на кастомном backend;
- хранение и восстановление сессии по bearer token;
- профиль пользователя;
- экран авторизации с несколькими состояниями;
- генератор auth-сцены;
- основной экран приложения;
- AR-сцена и image tracking;
- локальные quiz-механики в Unity;
- backend-модуль квизов;
- backend-модуль прогресса и достижений;
- художественный/рисовальный модуль;
- мини-игры;
- PostgreSQL schema для пользовательского прогресса.

## 24. Что ещё не доведено до конца

Есть зоны, где система уже подготовлена, но интеграция ещё не завершена:

- Unity quiz UI ещё не переведён полностью на backend quiz API;
- смена аватара в Unity пока не доведена до полноценного file upload flow;
- смена пароля требует отдельного UI с `current password`;
- часть legacy-скриптов и сцен ещё нуждается в дополнительной чистке и унификации;
- README описывает текущую реализованную структуру, но не заменяет сценовую инспекцию в Unity для точной вёрстки.

## 25. Рекомендуемый порядок онбординга

Если ты только подключаешься к проекту:

1. Прочитай этот README целиком.
2. Открой `elysia-app/src/index.ts`.
3. Посмотри `elysia-app/prisma/schema.prisma`.
4. Открой `unity/Assets/Scenes/Registration.unity`.
5. В Unity выбери объект `Systems` и изучи `BackendManager`.
6. Потом открой `RegistrationSceneBuilder.cs`.
7. После этого переходи к `The main stage`, `LobbyManager`, `QuizManager`, `Painter` и AR-сцене.

Такой порядок быстрее всего даёт целостное понимание проекта.
