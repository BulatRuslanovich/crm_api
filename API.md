# API Reference

REST API фармацевтической CRM-системы. Все ответы и запросы в формате JSON.
Поля сериализуются в `camelCase`.

## Содержание

- [Общие соглашения](#общие-соглашения)
- [Auth](#auth) — `/api/auth`
- [Users](#users) — `/api/users`
- [Departments](#departments) — `/api/departments`
- [Organizations](#organizations) — `/api/orgs`
- [Contacts](#contacts) — `/api/physes`
- [Drugs](#drugs) — `/api/drugs`
- [Activities](#activities) — `/api/activs`
- [Health & Metrics](#health--metrics)

---

## Общие соглашения

### Аутентификация

Все эндпоинты (кроме `/api/auth/*` и health) требуют заголовок:

```
Authorization: Bearer <access_token>
```

### Формат ошибок

Все ошибки возвращаются в формате [RFC 7807 Problem Details](https://datatracker.ietf.org/doc/html/rfc7807):

```typescript
interface ProblemDetails {
  status: number;
  title: string;
  type?: string;
  detail?: string;
  instance?: string;
  [key: string]: unknown; // дополнительные расширения
}
```

**Пример:**

```json
{
  "status": 404,
  "title": "User not found"
}
```

### Пагинация

```typescript
interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
```

Query-параметры: `page` (default: 1), `pageSize` (default: 20, max обычно 100).

### Роли и доступ

Доступные роли: `Admin`, `Director`, `Manager`, `Representative`.
Ниже в описании каждого эндпоинта указано, кто может его вызывать.

---

## Auth

> **Rate limit:** 10 запросов / минуту на IP.

### POST /api/auth/register

Регистрирует пользователя и отправляет 6-значный OTP-код на email. Аккаунт должен быть подтверждён до входа.

**Request Body:**

```typescript
interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  login: string;
  password: string;
}
```

**Example Request:**

```json
{
  "firstName": "Иван",
  "lastName": "Петров",
  "email": "ivan.petrov@example.com",
  "login": "ipetrov",
  "password": "strongPassword123"
}
```

**Success Response (202 Accepted):**

```json
{
  "email": "ivan.petrov@example.com"
}
```

**Error Responses:**

```json
// 400 — ошибка валидации (FluentValidation)
{
  "status": 400,
  "title": "Validation failed",
  "errors": { "email": ["Некорректный email"] }
}

// 409 — email или login уже занят
{
  "status": 409,
  "title": "User already exists"
}
```

---

### POST /api/auth/confirm-email

Подтверждает регистрацию по OTP-коду. Возвращает пару JWT-токенов.

**Request Body:**

```typescript
interface ConfirmEmailRequest {
  email: string;
  code: string; // 6-значный OTP
}
```

**Example Request:**

```json
{
  "email": "ivan.petrov@example.com",
  "code": "483920"
}
```

**Success Response (200):**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "d3b07384d113edec49eaa6238ad5ff00...",
  "user": {
    "usrId": 42,
    "firstName": "Иван",
    "lastName": "Петров",
    "email": "ivan.petrov@example.com",
    "login": "ipetrov",
    "policies": ["Representative"]
  }
}
```

**Error Responses:**

```json
// 400 — код неверный или просрочен
{
  "status": 400,
  "title": "Invalid or expired code"
}
```

---

### POST /api/auth/resend-confirmation

Отправляет новый OTP-код на указанный email. Всегда возвращает 204 — защита от перебора адресов.

**Request Body:** строка email.

```json
"ivan.petrov@example.com"
```

**Success Response (204):** пустое тело.

---

### POST /api/auth/forgot-password

Запрашивает сброс пароля. Отправляет OTP на email (TTL 1 час). Всегда 204.

**Request Body:**

```typescript
interface ForgotPasswordRequest {
  email: string;
}
```

**Example Request:**

```json
{ "email": "ivan.petrov@example.com" }
```

**Success Response (204):** пустое тело.

---

### POST /api/auth/reset-password

Устанавливает новый пароль по OTP-коду. Все refresh-токены пользователя инвалидируются.

**Request Body:**

```typescript
interface ResetPasswordRequest {
  email: string;
  code: string;
  newPassword: string;
}
```

**Example Request:**

```json
{
  "email": "ivan.petrov@example.com",
  "code": "831209",
  "newPassword": "newStrongPassword456"
}
```

**Success Response (204):** пустое тело.

**Error Responses:**

```json
// 400 — неверный или просроченный код
{ "status": 400, "title": "Invalid or expired code" }
```

---

### POST /api/auth/login

Аутентификация по логину и паролю.

**Request Body:**

```typescript
interface LoginRequest {
  login: string;
  password: string;
}
```

**Example Request:**

```json
{ "login": "ipetrov", "password": "strongPassword123" }
```

**Success Response (200):** см. [`AuthResponse`](#post-apiauthconfirm-email).

**Error Responses:**

```json
// 401 — неверные креды или email не подтверждён
{ "status": 401, "title": "Invalid credentials" }
```

---

### POST /api/auth/refresh

Обменивает refresh-токен на новую пару токенов. Старый refresh-токен становится невалидным.

**Request Body:** строка с refresh-токеном.

```json
"d3b07384d113edec49eaa6238ad5ff00..."
```

**Success Response (200):** см. [`AuthResponse`](#post-apiauthconfirm-email).

**Error Responses:**

```json
// 401 — токен неверный, просрочен или уже использован
{ "status": 401, "title": "Invalid refresh token" }
```

---

### POST /api/auth/logout

Инвалидирует указанный refresh-токен. Требует авторизацию.

**Request Body:** строка с refresh-токеном.

**Success Response (204):** пустое тело.

---

## Users

### GET /api/users

**Доступ:** `Admin`.
Возвращает список пользователей с их политиками.

**Query:**

| Параметр | Тип | Default | Описание |
|---|---|---|---|
| `page` | `number` | `1` | Номер страницы |
| `pageSize` | `number` | `20` | Размер (1..100) |

**Success Response (200):**

```typescript
interface UserResponse {
  usrId: number;
  firstName: string;
  lastName: string;
  email: string;
  login: string;
  policies: string[];
}

type Response = PagedResponse<UserResponse>;
```

---

### GET /api/users/{id}

**Доступ:** `Admin` или сам владелец.
Возвращает профиль пользователя по ID.

**Success Response (200):** [`UserResponse`](#get-apiusers).

**Error Responses:**

```json
// 403 — не админ и не владелец
// 404 — пользователь не найден
{ "status": 404, "title": "User not found" }
```

---

### GET /api/users/me

Возвращает профиль текущего пользователя.

**Success Response (200):** [`UserResponse`](#get-apiusers).

---

### POST /api/users

**Доступ:** `Admin`.
Создаёт пользователя с указанными политиками.

**Request Body:**

```typescript
interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  login: string;
  password: string;
  policyIds: number[];
}
```

**Example Request:**

```json
{
  "firstName": "Анна",
  "lastName": "Сидорова",
  "email": "anna@example.com",
  "login": "asidorova",
  "password": "temp12345",
  "policyIds": [2]
}
```

**Success Response (201):** [`UserResponse`](#get-apiusers). Заголовок `Location` указывает на `/api/users/{id}`.

**Error Responses:**

```json
// 409 — email или login заняты
{ "status": 409, "title": "User already exists" }
```

---

### PUT /api/users/{id}

**Доступ:** `Admin` или сам владелец.
Обновляет профиль пользователя (partial: `null` = не менять).

**Request Body:**

```typescript
interface UpdateUserRequest {
  firstName?: string | null;
  lastName?: string | null;
}
```

**Success Response (200):** [`UserResponse`](#get-apiusers).

---

### PATCH /api/users/{id}/password

**Доступ:** `Admin` или сам владелец.
Изменяет пароль пользователя.

**Request Body:**

```typescript
interface ChangePasswordRequest {
  oldPassword: string;
  newPassword: string;
}
```

**Success Response (204):** пустое тело.

**Error Responses:**

```json
// 401 — неверный старый пароль
{ "status": 401, "title": "Invalid credentials" }
```

---

### DELETE /api/users/{id}

**Доступ:** `Admin`.
Soft-удаляет пользователя и отзывает все его refresh-токены.

**Success Response (204):** пустое тело.

---

### GET /api/users/policies

Список всех политик (ролей). Кэшируется на 10 минут.

**Success Response (200):**

```typescript
interface PolicyResponse {
  policyId: number;
  policyName: string;
}

type Response = PolicyResponse[];
```

```json
[
  { "policyId": 1, "policyName": "Admin" },
  { "policyId": 2, "policyName": "Representative" },
  { "policyId": 3, "policyName": "Manager" },
  { "policyId": 4, "policyName": "Director" }
]
```

---

### GET /api/users/policies/{id}

Политика по ID.

**Success Response (200):** [`PolicyResponse`](#get-apiuserspolicies).

---

### POST /api/users/{id}/policies/{policyId}

**Доступ:** `Admin`.
Назначает политику пользователю.

**Success Response (200):** [`UserResponse`](#get-apiusers).

---

### DELETE /api/users/{id}/policies/{policyId}

**Доступ:** `Admin`.
Убирает политику у пользователя.

**Success Response (200):** [`UserResponse`](#get-apiusers).

---

## Departments

> **Доступ:** все эндпоинты требуют роль `Admin`.

### GET /api/departments

**Query:** `page` (default: 1), `pageSize` (default: 50, max 200).

**Success Response (200):**

```typescript
interface DepartmentResponse {
  departmentId: number;
  departmentName: string;
  userCount: number;
}

type Response = PagedResponse<DepartmentResponse>;
```

---

### GET /api/departments/{id}

**Success Response (200):** [`DepartmentResponse`](#get-apidepartments).

---

### POST /api/departments

**Request Body:**

```typescript
interface CreateDepartmentRequest {
  departmentName: string;
}
```

**Example Request:**

```json
{ "departmentName": "Северо-Запад" }
```

**Success Response (201):** [`DepartmentResponse`](#get-apidepartments).

**Error Responses:**

```json
// 409 — отдел с таким именем уже существует
{ "status": 409, "title": "Department already exists" }
```

---

### DELETE /api/departments/{id}

Soft-удаление.

**Success Response (204):** пустое тело.

---

### POST /api/departments/{id}/users/{usrId}

Добавляет пользователя в отдел.

**Success Response (204):** пустое тело.

**Error Responses:**

```json
// 404 — отдел или пользователь не найден
// 409 — пользователь уже в этом отделе
```

---

### DELETE /api/departments/{id}/users/{usrId}

Убирает пользователя из отдела.

**Success Response (204):** пустое тело.

---

## Organizations

### GET /api/orgs

Список организаций (пагинация + поиск по имени/ИНН/адресу).

**Query:** `page`, `pageSize` (1..100), `search`.

**Success Response (200):**

```typescript
interface OrgResponse {
  orgId: number;
  orgTypeId: number;
  orgTypeName: string;
  orgName: string;
  inn: string;
  latitude: number;
  longitude: number;
  address: string;
}

type Response = PagedResponse<OrgResponse>;
```

---

### GET /api/orgs/{id}

**Success Response (200):** [`OrgResponse`](#get-apiorgs).

---

### POST /api/orgs

**Доступ:** `Admin`.

**Request Body:**

```typescript
interface CreateOrgRequest {
  orgTypeId: number;
  orgName: string;
  inn: string;
  latitude: number;
  longitude: number;
  address: string;
}
```

**Example Request:**

```json
{
  "orgTypeId": 1,
  "orgName": "Аптека №5",
  "inn": "7712345678",
  "latitude": 55.7558,
  "longitude": 37.6173,
  "address": "Москва, ул. Тверская, 10"
}
```

**Success Response (201):** [`OrgResponse`](#get-apiorgs).

---

### PUT /api/orgs/{id}

**Доступ:** `Admin`. Partial: `null` = не менять.

**Request Body:**

```typescript
interface UpdateOrgRequest {
  orgTypeId?: number | null;
  orgName?: string | null;
  inn?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  address?: string | null;
}
```

**Success Response (200):** [`OrgResponse`](#get-apiorgs).

---

### DELETE /api/orgs/{id}

**Доступ:** `Admin`. Soft-удаление.

**Success Response (204):** пустое тело.

---

### GET /api/orgs/types

Список типов организаций. Кэш 10 минут.

**Success Response (200):**

```typescript
interface OrgTypeResponse {
  orgTypeId: number;
  orgTypeName: string;
}

type Response = OrgTypeResponse[];
```

---

## Contacts

Физические лица (врачи, контактные лица организаций).

### GET /api/physes

**Query:** `page`, `pageSize` (1..100), `search`.

**Success Response (200):**

```typescript
interface PhysResponse {
  physId: number;
  specId: number;
  specName: string;
  firstName: string;
  lastName: string;
  middleName: string | null;
  phone: string | null;
  email: string;
  orgs: OrgResponse[];
}

type Response = PagedResponse<PhysResponse>;
```

---

### GET /api/physes/{id}

**Success Response (200):** [`PhysResponse`](#get-apiphyses).

---

### POST /api/physes

**Доступ:** `Admin`.

**Request Body:**

```typescript
interface CreatePhysRequest {
  specId: number;
  firstName: string;
  lastName: string;
  middleName: string;
  phone: string;
  email: string;
}
```

**Example Request:**

```json
{
  "specId": 3,
  "firstName": "Мария",
  "lastName": "Иванова",
  "middleName": "Петровна",
  "phone": "+79991234567",
  "email": "ivanova@clinic.ru"
}
```

**Success Response (201):** [`PhysResponse`](#get-apiphyses).

---

### PUT /api/physes/{id}

**Доступ:** `Admin`. Partial.

**Request Body:**

```typescript
interface UpdatePhysRequest {
  specId?: number | null;
  firstName?: string | null;
  lastName?: string | null;
  middleName?: string | null;
  phone?: string | null;
  email?: string | null;
}
```

**Success Response (200):** [`PhysResponse`](#get-apiphyses).

---

### DELETE /api/physes/{id}

**Доступ:** `Admin`. Soft-удаление.

**Success Response (204):** пустое тело.

---

### POST /api/physes/{physId}/orgs/{orgId}

**Доступ:** `Admin`. Привязывает контакт к организации.

**Success Response (204):** пустое тело.

---

### DELETE /api/physes/{physId}/orgs/{orgId}

**Доступ:** `Admin`. Отвязывает.

**Success Response (204):** пустое тело.

---

### GET /api/physes/specs

Список специализаций. Кэш 10 минут.

**Success Response (200):**

```typescript
interface SpecResponse {
  specId: number;
  specName: string;
}

type Response = SpecResponse[];
```

---

### GET /api/physes/specs/{id}

**Success Response (200):** [`SpecResponse`](#get-apiphysesspecs).

---

### POST /api/physes/specs

**Доступ:** `Admin`.

**Request Body:**

```typescript
interface CreateSpecRequest {
  specName: string;
}
```

**Success Response (201):** [`SpecResponse`](#get-apiphysesspecs).

---

### DELETE /api/physes/specs/{id}

**Доступ:** `Admin`.

**Success Response (204):** пустое тело.

---

## Drugs

### GET /api/drugs

**Query:** `page`, `pageSize` (1..100), `search`.

**Success Response (200):**

```typescript
interface DrugResponse {
  drugId: number;
  drugName: string;
  brand: string;
  form: string;
}

type Response = PagedResponse<DrugResponse>;
```

---

### GET /api/drugs/{id}

**Success Response (200):** [`DrugResponse`](#get-apidrugs).

---

### POST /api/drugs

**Доступ:** `Admin`.

**Request Body:**

```typescript
interface CreateDrugRequest {
  drugName: string;
  brand: string;
  form: string;
}
```

**Example Request:**

```json
{
  "drugName": "Амоксициллин",
  "brand": "Флемоксин Солютаб",
  "form": "таблетки 500мг"
}
```

**Success Response (201):** [`DrugResponse`](#get-apidrugs).

---

### PUT /api/drugs/{id}

**Доступ:** `Admin`. Partial.

**Request Body:**

```typescript
interface UpdateDrugRequest {
  drugName?: string | null;
  brand?: string | null;
  form?: string | null;
}
```

**Success Response (200):** [`DrugResponse`](#get-apidrugs).

---

### DELETE /api/drugs/{id}

**Доступ:** `Admin`. Soft-удаление.

**Success Response (204):** пустое тело.

---

## Activities

Доступ к активностям ограничен **scope**, зависящим от роли:

| Роль | Scope |
|---|---|
| `Admin`, `Director` | все активности |
| `Manager` | активности пользователей своего отдела |
| `Representative` | только свои активности |

### GET /api/activs

Список активностей с фильтрами и сортировкой.

**Query:**

```typescript
interface ActivQuery {
  page?: number;           // default 1
  pageSize?: number;       // default 100; max 500, или 5000 если указан dateFrom
  search?: string;
  sortBy?: "Start" | "End" | "Status";
  sortDesc?: boolean;
  statuses?: number[];     // ?statuses=1&statuses=2
  dateFrom?: string;       // ISO 8601 DateTimeOffset
  dateTo?: string;
}
```

**Success Response (200):**

```typescript
interface ActivResponse {
  activId: number;
  usrId: number;
  usrLogin: string;
  orgId: number | null;
  orgName: string | null;
  physId: number | null;
  physName: string | null;
  statusId: number;
  statusName: string;
  start: string | null;   // ISO 8601
  end: string | null;
  description: string;
  drugs: DrugResponse[];
}

type Response = PagedResponse<ActivResponse>;
```

---

### GET /api/activs/{id}

**Success Response (200):** [`ActivResponse`](#get-apiactivs).

**Error Responses:**

```json
// 404 — активность вне scope или не существует
{ "status": 404, "title": "Activity not found" }
```

---

### POST /api/activs

Создаёт активность от имени текущего пользователя.

**Request Body:**

```typescript
interface CreateActivRequest {
  orgId: number | null;
  physId: number | null;
  statusId: number;
  start: string;           // ISO 8601
  end: string | null;
  description: string;
  drugIds: number[];
}
```

**Example Request:**

```json
{
  "orgId": 42,
  "physId": 17,
  "statusId": 1,
  "start": "2026-04-20T10:00:00+03:00",
  "end": "2026-04-20T11:00:00+03:00",
  "description": "Презентация препарата X",
  "drugIds": [1, 5, 12]
}
```

**Success Response (201):** [`ActivResponse`](#get-apiactivs).

---

### PUT /api/activs/{id}

Partial update в пределах scope.

**Request Body:**

```typescript
interface UpdateActivRequest {
  statusId?: number | null;
  start?: string | null;
  end?: string | null;
  description?: string | null;
}
```

**Success Response (200):** [`ActivResponse`](#get-apiactivs).

---

### DELETE /api/activs/{id}

Soft-удаление в пределах scope.

**Success Response (204):** пустое тело.

---

### POST /api/activs/{activId}/drugs/{drugId}

Привязывает препарат к активности (в пределах scope).

**Success Response (204):** пустое тело.

---

### DELETE /api/activs/{activId}/drugs/{drugId}

Отвязывает препарат.

**Success Response (204):** пустое тело.

---

## Health & Metrics

### GET /health

Проверка работоспособности сервиса (БД + SMTP).

**Success Response (200):**

```
Healthy
```

**Error Response (503):** `Unhealthy`.

---

### GET /metrics

Prometheus-метрики в текстовом формате (`text/plain`).
