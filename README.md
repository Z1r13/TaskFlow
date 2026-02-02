# TaskFlow

REST API для управления задачами с JWT-авторизацией.

## Технологии

- **Backend:** ASP.NET Core (.NET 8), Minimal API
- **Database:** PostgreSQL, Entity Framework Core
- **Auth:** JWT (Bearer tokens)
- **Infrastructure:** Docker, Docker Compose
- **Testing:** xUnit, FluentAssertions, WebApplicationFactory

## Функционал

- ✅ Регистрация и аутентификация (JWT)
- ✅ CRUD для задач (только свои задачи пользователя)
- ✅ Миграции БД (EF Core)
- ✅ Интеграционные тесты
- ✅ Docker окружение (API + PostgreSQL)

## Быстрый старт

### Требования

- .NET 8 SDK
- Docker & Docker Compose

### Запуск

```bash
# Клонировать репозиторий
git clone https://github.com/Z1r13/TaskFlow.git
cd TaskFlow

# Создать .env файл (или использовать значения по умолчанию в docker-compose.yml)
cp .env.example .env

# Запустить контейнеры
docker compose up

# API доступен на http://localhost:5000
# Swagger UI: http://localhost:5000/swagger
```

### Запуск тестов

```bash
dotnet test
```

## API Endpoints

### Auth
- `POST /auth/register` — регистрация
- `POST /auth/login` — получение JWT токена

### Tasks (требуется авторизация)
- `GET /tasks` — список задач
- `POST /tasks` — создать задачу
- `PUT /tasks/{id}` — обновить задачу
- `DELETE /tasks/{id}` — удалить задачу


## Статус проекта

**Work in Progress** — учебный проект для демонстрации навыков работы с .NET-стеком.

Основные фичи реализованы, проект может быть расширен (добавление валидации, пагинации, фильтров и т.д.).

## Лицензия

MIT
