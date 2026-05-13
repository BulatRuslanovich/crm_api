<div align="center">

# CRM API

**REST API фармацевтической CRM-системы**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-optional-DC382D?logo=redis&logoColor=white)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://docs.docker.com/compose/)

</div>

---

## О проекте

Бэкенд для CRM фарм компаний.


## Стек технологий

| Категория      | Технологии                                                  |
|----------------|-------------------------------------------------------------|
| Runtime        | .NET 10, ASP.NET Core                                       |
| База данных    | PostgreSQL 17                                               |
| ORM            | Entity Framework Core 9                                     |
| Аутентификация | JWT Bearer                                                  |
| Кэширование    | HybridCache; in-memory локально, Redis в production compose |
| Email          | MailKit (Gmail SMTP)                                        |
| Документация   | OpenAPI + Scalar                                            |
| Инфраструктура | Docker, Caddy, GitHub Actions CI, Pharmo Web, Grafana       |


## API

| Раздел        | Префикс               | Описание                                            |
|---------------|-----------------------|-----------------------------------------------------|
| Auth          | `/api/auth`           | регистрация, вход, OTP, refresh (rate limit 10/min) |
| Users         | `/api/users`          | пользователи и политики доступа                     |
| Departments   | `/api/departments`    | отделы и их участники (Admin only)                  |
| Organizations | `/api/orgs`           | организации, типы                                   |
| Contacts      | `/api/physes`         | физлица, специализации, связи с орг.                |
| Drugs         | `/api/drugs`          | препараты                                           |
| Activities    | `/api/activs`         | визиты/активности со scope-доступом                 |
| Health        | `/health`, `/metrics` | health check, Prometheus                            |

**Ролевая модель для активностей:**

| Роль                | Scope                                  |
|---------------------|----------------------------------------|
| `Admin`, `Director` | все активности                         |
| `Manager`           | активности пользователей своего отдела |
| `Representative`    | только свои активности                 |

Защищённые эндпоинты требуют заголовок `Authorization: Bearer <access_token>`.
Scalar UI: `http://localhost:5000/scalar/v1`.

## Demo

Единый compose для локальной демонстрации: API + frontend + Caddy + Postgres + Redis + Grafana/Loki/Prometheus. AI-ассистент использует DeepSeek-compatible API.

1. Положите рядом с этим репозиторием чекаут фронтенда: `../crm-web-portal` (или укажите путь через `FRONTEND_PATH`).
2. Скопируйте шаблон env и заполните значения:
   ```bash
   cp .env.example .env.local
   ```
   Для AI-ассистента задайте `DEEPSEEK_API_KEY`; при необходимости можно переопределить `DEEPSEEK_BASE_URL` и `DEEPSEEK_MODEL`.
3. Поднимите стек:
   ```bash
   docker compose --env-file .env.local -f compose.demo.yml up -d --build
   ```

API и фронт собираются локально из исходников. Если в `.env.local` не задана хотя бы одна обязательная переменная — compose упадёт сразу с явной ошибкой `is required in .env.local`.

| URL                          | Что                              |
|------------------------------|----------------------------------|
| http://localhost             | Frontend (Caddy → frontend:3000) |
| http://localhost/api/...     | API (Caddy → api:8080)           |
| http://localhost:3001        | Grafana (admin / `${GRAFANA_ADMIN_PASSWORD}`) |
| localhost:5432               | Postgres (`crm_user` / `${DB_PASSWORD}`) |

## Tests

Тестовый проект подключён к solution: `CrmWebApi.Tests/CrmWebApi.Tests.csproj`.
Он содержит unit-тесты и contract smoke-тесты для auth, authorization, health и основных контроллеров.

```bash
dotnet test
```


- **[DEV.md](DEV.md)** — памятка разработчику: как поднять проект локально
