<div align="center">

# CRM API

**REST API фармацевтической CRM-системы**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://docs.docker.com/compose/)

</div>

---

## О проекте

Бэкенд для CRM фарм компаний. Поддерживает ролевую модель доступа, email-подтверждение.


## Стек технологий

| Категория | Технологии |
|---|---|
| Runtime | .NET 10, ASP.NET Core |
| База данных | PostgreSQL 17, Entity Framework Core 9 |
| Аутентификация | JWT Bearer, BCrypt, Email OTP |
| Валидация | FluentValidation |
| Кэширование | HybridCache (in-memory + distributed) |
| Логирование | Serilog |
| Email | MailKit (Gmail SMTP) |
| Документация | OpenAPI + Scalar |
| Инфраструктура | Docker, Caddy, GitHub Actions |

## Архитектура

```
Controllers  →  Services  →  Repositories  →  EF Core / PostgreSQL
```

## API

Полная документация всех эндпоинтов с TypeScript-интерфейсами запросов/ответов и примерами —
**→ [API.md](API.md)**

| Раздел | Префикс | Описание |
|---|---|---|
| Auth | `/api/auth` | регистрация, вход, OTP, refresh (rate limit 10/min) |
| Users | `/api/users` | пользователи и политики доступа |
| Departments | `/api/departments` | отделы и их участники (Admin only) |
| Organizations | `/api/orgs` | организации, типы |
| Contacts | `/api/physes` | физлица, специализации, связи с орг. |
| Drugs | `/api/drugs` | препараты |
| Activities | `/api/activs` | визиты/активности со scope-доступом |
| Health | `/health`, `/metrics` | health check, Prometheus |

**Ролевая модель для активностей:**

| Роль | Scope |
|---|---|
| `Admin`, `Director` | все активности |
| `Manager` | активности пользователей своего отдела |
| `Representative` | только свои активности |

Защищённые эндпоинты требуют заголовок `Authorization: Bearer <access_token>`.
В dev-режиме доступен Scalar UI: `http://localhost:5000/scalar/v1`.

## Документация

- **[API.md](API.md)** — справочник REST API: все эндпоинты, TS-интерфейсы, примеры
- **[DEV.md](DEV.md)** — памятка разработчику: как поднять проект локально

---

<div align="center">

MIT © [BulatRuslanovich](https://github.com/BulatRuslanovich)

</div>
