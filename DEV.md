# Локальный запуск

Памятка для разработчиков: как поднять проект на своей машине.

## Содержание

- [Требования](#требования)
- [Клонирование и восстановление зависимостей](#1-клонирование-и-восстановление-зависимостей)
- [Поднять инфраструктуру](#2-поднять-инфраструктуру)
- [Настроить секреты](#3-настроить-секреты)
- [Запуск](#4-запуск)
- [Проверка](#5-проверка)
- [Структура проекта](#структура-проекта)
- [Частые проблемы](#частые-проблемы)

---

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (SDK версии ≥ 10.0.100)
- [Docker](https://docs.docker.com/get-docker/) — для PostgreSQL; Redis поднимается production compose
- (опционально) [Python](https://https://www.python.org/) — для mock-данных

Проверить SDK:

```bash
dotnet --version
# 10.0.xxx
```

---

## 1. Клонирование и восстановление зависимостей

```bash
git clone https://github.com/BulatRuslanovich/crm_api.git
cd crm_api
dotnet restore
```

---

## 2. Поднять инфраструктуру

Для разработки есть отдельный compose-файл с Postgres на `localhost:5432`:

```bash
docker compose -f compose.dev.yml up -d
```

**Креды по умолчанию:**

| Параметр | Значение |
|---|---|
| Host | `localhost` |
| Port | `5432` |
| Database | `crm_db` |
| User | `crm_user` |
| Password | `${DB_PASSWORD:-change_me_dev_password}` |

SQL-скрипты из папки [`sql-scripts/`](sql-scripts/) автоматически применяются при первом запуске контейнера.


Production compose дополнительно поднимает Redis и передает API `Cache__RedisConnectionString`. Локально Redis не обязателен: без `Cache:RedisConnectionString` используется in-memory HybridCache.

**Сброс БД** (полностью удалит данные):

```bash
docker compose -f compose.dev.yml down -v
docker compose -f compose.dev.yml up -d
```

**Генерация mock данных**:

```bash
python -n venv venv
source .venv/bin/activate.fish # скрипт зависит от оболочки или ос
pip install faker psycopg2-binary
python generate-fake-date.py
```

---

## 3. Настроить секреты

Используйте **user-secrets** — значения хранятся вне репозитория:

```bash
dotnet user-secrets init

dotnet user-secrets set "ConnectionStrings:Default" \
  "Host=localhost;Port=5432;Database=crm_db;Username=crm_user;Password=<локальный пароль БД>"

dotnet user-secrets set "Jwt:Secret" \
  "любая_случайная_строка_минимум_32_символа!!"

dotnet user-secrets set "Auth:OtpHashSecret" \
  "отдельная_случайная_строка_минимум_32_символа"

dotnet user-secrets set "Email:Username" "<smtp логин>"
dotnet user-secrets set "Email:Password" "<пароль приложения Gmail>"
dotnet user-secrets set "Email:FromAddress" "<адрес отправителя>"
```

> **Требования:**
> - `Jwt:Secret` обязателен и должен быть **минимум 32 символа**.
> - `Auth:OtpHashSecret` опционален; если не задан, OTP-хэши подписываются `Jwt:Secret`.
> - `Email:Username`, `Email:Password`, `Email:FromAddress` нужны только если тестируете OTP-флоу (регистрация, сброс пароля). Без них приложение стартует, но email-эндпоинты вернут 500.
> - `Cache:RedisConnectionString` опционален для локальной разработки.

Посмотреть / очистить:

```bash
dotnet user-secrets list
dotnet user-secrets clear
```

---

## 4. Запуск

**Обычный запуск:**

```bash
dotnet run
```

**С hot-reload при изменениях:**

```bash
dotnet watch
```

**Дебаг в VS Code:** `F5` — конфигурация в [`.vscode/launch.json`](.vscode/launch.json).
Конфигурации:
- `CrmWebApi (Debug)` — запуск с точками останова
- `CrmWebApi (Attach)` — подключение к уже запущенному процессу

**Дебаг в Rider / Visual Studio:** стандартная кнопка запуска — они сами подхватят `launchSettings.json`.

API поднимется на `http://localhost:5000`.
Scalar UI: [`http://localhost:5000/scalar/v1`](http://localhost:5000/scalar/v1).

---

## 5. Проверка

**Health check:**

```bash
curl http://localhost:5000/health
# Healthy
```

**Регистрация тестового пользователя:**

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Test",
    "lastName": "User",
    "email": "test@example.com",
    "login": "test",
    "password": "password123"
  }'
```


**Быстрая проверка качества:**

```bash
dotnet format CrmWebApi.sln --no-restore --verify-no-changes
dotnet build CrmWebApi.sln --no-restore
dotnet test CrmWebApi.sln --no-restore --no-build
dotnet list CrmWebApi.sln package --vulnerable --include-transitive
```

---

## Структура проекта

```
CrmWebApi/
├── Controllers/      — HTTP-эндпоинты
├── Services/         — бизнес-логика
├── Repositories/     — доступ к данным (EF Core)
├── Data/             — DbContext, Entities, конфигурации
├── DTOs/             — DTO запросов/ответов
├── Validators/       — FluentValidation-правила
├── Filters/          — MVC-фильтры (валидация)
├── Extensions/       — регистрация сервисов, JWT, DB
├── Exceptions/       — глобальные обработчики
├── Health/           — health-чеки (SMTP)
├── Common/           — Result<T>, Error, enums
├── Options/          — typed options и конфигурационная валидация
├── CrmWebApi.Tests/  — unit tests
├── sql-scripts/      — init-схема для Postgres
└── Program.cs        — точка входа, pipeline
```

---

## Частые проблемы

### `NETSDK1226: данные пакета Prune не найдены`

В `.csproj` уже стоит `<AllowMissingPrunePackageData>true</AllowMissingPrunePackageData>`.
Если ошибка всё равно появляется — проверьте, что установленный .NET SDK соответствует `TargetFramework` в `.csproj` (`net10.0`).

### `connection refused` к БД

Контейнер не запущен:

```bash
docker compose -f compose.dev.yml ps
docker compose -f compose.dev.yml logs db
```

### `JWT secret must be at least 32 chars`

Задайте `Jwt:Secret` через user-secrets (см. [шаг 3](#3-настроить-секреты)).
