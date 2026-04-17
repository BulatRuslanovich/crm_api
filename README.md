# CRM Web API

REST API для фармацевтической CRM-системы.

## Содержание

- [Стек технологий](#стек-технологий)
- [Архитектура](#архитектура)
- [Запуск](#запуск)
- [Переменные окружения](#переменные-окружения)
- [API](#api)
- [Аутентификация](#аутентификация)
- [CI/CD](#cicd)

---

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

---

## Архитектура

```
Controllers
    └── вызывают Services
Services
    └── вызывают Repositories
Repositories
    └── работают с EF Core / PostgreSQL
```

**Паттерны:**
- Repository + Service — разделение бизнес-логики и доступа к данным
- `Result<T>` — railway-oriented обработка ошибок без исключений
- Soft delete — записи помечаются удалёнными через `IsDeleted`, а не физически удаляются
- Problem Details (RFC 7807) — единый формат ошибок во всех ответах
- FluentValidation + action filter — автоматическая валидация запросов до входа в контроллер

---

## Запуск

### Через Docker Compose

```bash
cp .env.example .env
# заполнить переменные в .env
docker compose up -d
```

Сервисы:

| Сервис | Порт |
|---|---|
| API | 8080 (внутренний, за Caddy) |
| PostgreSQL | 5432 |
| Frontend | 3000 (внутренний, за Caddy) |
| Caddy | 80, 443 |

### Локально (без Docker)

```bash
dotnet restore
dotnet run
```

Swagger UI доступен по адресу `/scalar/v1` в режиме Development.

---

## Переменные окружения

| Переменная | Описание |
|---|---|
| `DB_PASSWORD` | Пароль PostgreSQL |
| `JWT_SECRET` | Секрет для подписи JWT |
| `EMAIL_PASSWORD` | Пароль приложения Gmail |
| `CRMWEBAPI_IMAGE` | Docker-образ API (проставляется CI/CD) |

---

## API

Все эндпоинты, кроме помеченных, требуют заголовок:

```
Authorization: Bearer <access_token>
```

### Auth `/api/auth`

> Rate limit: 10 запросов в минуту

| Метод | Путь | Описание | Auth |
|---|---|---|---|
| POST | `/register` | Регистрация + отправка OTP на email | Нет |
| POST | `/confirm-email` | Подтверждение email по OTP | Нет |
| POST | `/resend-confirmation` | Повторная отправка OTP | Нет |
| POST | `/forgot-password` | Запрос сброса пароля | Нет |
| POST | `/reset-password` | Сброс пароля по OTP | Нет |
| POST | `/login` | Вход по логину и паролю | Нет |
| POST | `/refresh` | Обновление токенов | Нет |
| POST | `/logout` | Инвалидация refresh-токена | Да |

### Users `/api/users`

| Метод | Путь | Описание | Роль |
|---|---|---|---|
| GET | `/` | Список пользователей (пагинация) | Admin |
| GET | `/{id}` | Пользователь по ID | Admin / Владелец |
| GET | `/me` | Текущий пользователь | Все |
| POST | `/` | Создать пользователя | Admin |
| PUT | `/{id}` | Обновить пользователя | Admin |
| PATCH | `/{id}/password` | Сменить пароль | Admin / Владелец |
| DELETE | `/{id}` | Удалить пользователя | Admin |
| GET | `/policies` | Список ролей | Все |
| GET | `/policies/{id}` | Роль по ID | Все |
| POST | `/{id}/policies/{policyId}` | Назначить роль | Admin |
| DELETE | `/{id}/policies/{policyId}` | Убрать роль | Admin |

### Organizations `/api/orgs`

| Метод | Путь | Описание | Роль |
|---|---|---|---|
| GET | `/` | Список организаций (пагинация) | Все |
| GET | `/{id}` | Организация по ID | Все |
| POST | `/` | Создать организацию | Admin |
| PUT | `/{id}` | Обновить организацию | Admin |
| DELETE | `/{id}` | Удалить организацию | Admin |
| GET | `/types` | Типы организаций | Все |

### Contacts `/api/physes`

| Метод | Путь | Описание | Роль |
|---|---|---|---|
| GET | `/` | Список контактов (пагинация) | Все |
| GET | `/{id}` | Контакт по ID | Все |
| POST | `/` | Создать контакт | Admin |
| PUT | `/{id}` | Обновить контакт | Admin |
| DELETE | `/{id}` | Удалить контакт | Admin |
| POST | `/{physId}/orgs/{orgId}` | Привязать контакт к организации | Admin |
| DELETE | `/{physId}/orgs/{orgId}` | Отвязать контакт от организации | Admin |
| GET | `/specs` | Список специализаций | Все |
| GET | `/specs/{id}` | Специализация по ID | Все |
| POST | `/specs` | Создать специализацию | Admin |
| DELETE | `/specs/{id}` | Удалить специализацию | Admin |

### Drugs `/api/drugs`

| Метод | Путь | Описание | Роль |
|---|---|---|---|
| GET | `/` | Список препаратов (пагинация) | Все |
| GET | `/{id}` | Препарат по ID | Все |
| POST | `/` | Создать препарат | Admin |
| PUT | `/{id}` | Обновить препарат | Admin |
| DELETE | `/{id}` | Удалить препарат | Admin |

### Activities `/api/activs`

| Метод | Путь | Описание | Роль |
|---|---|---|---|
| GET | `/` | Список активностей (пагинация, фильтрация) | Все (Admin видит все, остальные — свои) |
| GET | `/{id}` | Активность по ID | Admin / Владелец |
| POST | `/` | Создать активность | Все |
| PUT | `/{id}` | Обновить активность | Admin / Владелец |
| DELETE | `/{id}` | Удалить активность | Admin |
| POST | `/{activId}/drugs/{drugId}` | Привязать препарат | Admin / Владелец |
| DELETE | `/{activId}/drugs/{drugId}` | Отвязать препарат | Admin / Владелец |

### Health

| Метод | Путь | Описание |
|---|---|---|
| GET | `/health` | Проверка соединения с БД |

---

## Аутентификация

Используется схема JWT Bearer + Email OTP для подтверждения аккаунта.

**Флоу регистрации:**
1. `POST /api/auth/register` — аккаунт создаётся, на email уходит 6-значный OTP
2. `POST /api/auth/confirm-email` — OTP подтверждается, возвращаются токены

**Токены:**
- Access token — HS256, TTL 15 минут
- Refresh token — случайный Base64, TTL 7 дней, хранится в БД в виде SHA256-хэша, одноразовый

**Сброс пароля:**
1. `POST /api/auth/forgot-password` — OTP на email (TTL 1 час)
2. `POST /api/auth/reset-password` — OTP + новый пароль, все refresh-токены инвалидируются

---

## CI/CD

При пуше в `master` запускается GitHub Actions pipeline:

1. **Build & Push** — собирает Docker-образ, тегирует SHA коммита и `latest`, пушит в GHCR
2. **Deploy** — подключается по SSH к серверу, делает `git pull`, обновляет `.env` с новым тегом образа, перезапускает контейнеры через `docker compose up -d`
3. **Notify** — отправляет статус деплоя в Telegram

**Необходимые секреты в GitHub:**

| Секрет | Описание |
|---|---|
| `CR_PAT` | GitHub Container Registry токен |
| `SSH_HOST` | IP-адрес сервера |
| `SSH_PRIVATE_KEY` | Приватный SSH-ключ |
| `TELEGRAM_BOT_TOKEN` | Токен Telegram-бота |
| `TELEGRAM_CHAT_ID` | ID чата для уведомлений |
