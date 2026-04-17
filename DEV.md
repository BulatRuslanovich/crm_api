# Локальный запуск

Памятка для разработчиков: как поднять проект на своей машине.

## Содержание

- [Требования](#требования)
- [Клонирование и восстановление зависимостей](#1-клонирование-и-восстановление-зависимостей)
- [Поднять PostgreSQL](#2-поднять-postgresql)
- [Настроить секреты](#3-настроить-секреты)
- [Запуск](#4-запуск)
- [Проверка](#5-проверка)
- [Структура проекта](#структура-проекта)
- [Частые проблемы](#частые-проблемы)

---

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (SDK версии ≥ 10.0.100)
- [Docker](https://docs.docker.com/get-docker/) — для PostgreSQL
- (опционально) [Python](https://https://www.python.org/) — для mock-данных

Проверить SDK:

```bash
dotnet --version
# 10.0.xxx
```

---

## 1. Клонирование и восстановление зависимостей

```bash
git clone https://github.com/BulatRuslanovich/CrmWebApi.git
cd CrmWebApi
dotnet restore
```

---

## 2. Поднять PostgreSQL

Для разработки есть отдельный compose-файл с Postgres на `localhost:5432`:

```bash
docker compose -f docker-compose.dev.yml up -d
```

**Креды по умолчанию:**

| Параметр | Значение |
|---|---|
| Host | `localhost` |
| Port | `5432` |
| Database | `crm_db` |
| User | `crm_user` |
| Password | `12345678lol` |

SQL-скрипты из папки [`sql-scripts/`](sql-scripts/) автоматически применяются при первом запуске контейнера (схема ).

**Сброс БД** (полностью удалит данные):

```bash
docker compose -f docker-compose.dev.yml down -v
docker compose -f docker-compose.dev.yml up -d
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
  "Host=localhost;Port=5432;Database=crm_db;Username=crm_user;Password=12345678lol"

dotnet user-secrets set "Jwt:Secret" \
  "любая_случайная_строка_минимум_32_символа!!"

dotnet user-secrets set "Email:Password" "<пароль приложения Gmail>"
```

> **Требования:**
> - `Jwt:Secret` обязателен и должен быть **минимум 32 символа**.
> - `Email:Password` нужен только если тестируете OTP-флоу (регистрация, сброс пароля). Без него приложение стартует, но email-эндпоинты вернут 500.

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
Scalar UI (только в `Development`): [`http://localhost:5000/scalar/v1`](http://localhost:5000/scalar/v1).

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

Полная документация API — [API.md](API.md).

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
docker compose -f docker-compose.dev.yml ps
docker compose -f docker-compose.dev.yml logs db
```

### `JWT secret must be at least 32 chars`

Задайте `Jwt:Secret` через user-secrets (см. [шаг 3](#3-настроить-секреты)).

### Scalar UI не открывается

Scalar работает только в `Development`-окружении. Проверьте:

```bash
echo $ASPNETCORE_ENVIRONMENT
# Должно быть Development (или не задано — тогда используется Development по умолчанию при dotnet run)
```
