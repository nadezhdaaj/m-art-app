# Запуск backend через Docker

Ниже описан рабочий сценарий запуска backend из корня проекта `MART` с применением всех Prisma-миграций и сидов.

## 1. Подготовить переменные окружения

Если файла `api/.env` ещё нет, создайте его на основе `api/.env.example`.

Для запуска backend внутри Docker важно, чтобы `DATABASE_URL` указывал не на `localhost`, а на сервис `db`:

```env
DATABASE_URL="postgres://postgres:postgres@db:5432/mart"
```

Если в `api/.env` сейчас стоит `localhost`, замените его на `db`, иначе контейнер `api` не сможет подключиться к Postgres.

## 2. Поднять PostgreSQL

Из корня проекта выполните:

```powershell
docker compose up -d db
```

## 3. Применить миграции и сиды

В `docker-compose` уже добавлен отдельный сервис `migrate`, поэтому из корня проекта достаточно выполнить:

```powershell
docker compose run --rm migrate
```

Эта команда:

- устанавливает зависимости внутри временного контейнера;
- применяет все миграции из `api/prisma/migrations`;
- запускает сиды из `api/prisma/seed.ts`.

## 4. Поднять backend API

После миграций и сидов запустите backend:

```powershell
docker compose up --build -d api
```

API будет доступен на:

```text
http://localhost:3000
```

## 5. Остановить окружение 

```powershell
docker compose down
```

Если нужно удалить и данные Postgres volume:

```powershell
docker compose down -v
```
