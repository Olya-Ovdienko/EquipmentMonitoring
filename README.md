# EquipmentMonitoring

## Призначення

EquipmentMonitoring — ASP.NET Core Web API для реєстрації обладнання, збереження та перегляду вимірювань, налаштування порогів, роботи зі сповіщеннями й технічним обслуговуванням. Рішення цільово використовує **.NET 10** і побудоване з розділенням на доменний, прикладний, інфраструктурний та API-рівні.

Основна база даних — **PostgreSQL 17**. Доступ до неї реалізовано через **Npgsql** та **Entity Framework Core**. SQLite-провайдер залишається у складі Infrastructure для ізольованих тестових сценаріїв; Docker Compose і типовий запуск API використовують PostgreSQL.

## Архітектура рішення

`EquipmentMonitoring.slnx` містить п'ять проєктів:

- **EquipmentMonitoring.Domain** — доменні сутності, правила й типи предметної області.
- **EquipmentMonitoring.Application** — DTO, абстракції, сервіси та валідація запитів через FluentValidation.
- **EquipmentMonitoring.Infrastructure** — репозиторії, EF Core `EquipmentDbContext`, конфігурації сутностей, провайдери БД і міграції.
- **EquipmentMonitoring.API** — ASP.NET Core контролери, конфігурація залежностей та Swagger/OpenAPI.
- **EquipmentMonitoring.Tests** — модульні й інтеграційні тести на xUnit v3.

Ключові сценарії API охоплюють реєстрацію обладнання з перевіркою унікальності серійного номера, створення та фільтрацію вимірювань, а також маршрути для порогів, сповіщень і технічного обслуговування. Актуальний контракт API описаний у Swagger.

## Технології та вимоги

- .NET 10 SDK — для локальної розробки та запуску тестів.
- Docker Desktop із Docker Compose v2 — для запуску всього середовища в контейнерах.
- PostgreSQL 17 — основна база даних.
- Npgsql та Entity Framework Core — PostgreSQL provider і ORM.
- Visual Studio 2026 або інше сумісне середовище розробки .NET.

## Запуск через Docker Compose

Compose-конфігурація запускає три сервіси:

| Сервіс | Призначення | Адреса з хоста |
| --- | --- | --- |
| `api` | ASP.NET Core Web API | `http://localhost:8080` |
| `postgres` | PostgreSQL 17 | порт `5432` |
| `pgadmin` | Вебінтерфейс адміністрування PostgreSQL | `http://localhost:5050` |

### 1. Встановіть і запустіть Docker Desktop

Встановіть Docker Desktop для своєї операційної системи та дочекайтеся стану **Engine running**. Переконайтеся, що Docker Compose доступний:

```powershell
docker --version
docker compose version
```

### 2. Створіть локальний файл середовища

У корені репозиторію скопіюйте шаблон:

```powershell
Copy-Item .env.example .env
```

Відредагуйте `.env` і задайте власні локальні значення для змінних:

- `POSTGRES_DB` — назва бази (шаблон використовує `equipmentmonitoring`).
- `POSTGRES_USER` — користувач PostgreSQL.
- `POSTGRES_PASSWORD` — локальний пароль PostgreSQL.
- `PGADMIN_DEFAULT_EMAIL` — адреса входу до pgAdmin.
- `PGADMIN_DEFAULT_PASSWORD` — локальний пароль pgAdmin.

Замініть усі шаблонні паролі на власні тестові значення. Не додавайте `.env` або справжні паролі до Git. Файл `.env` уже виключений правилами `.gitignore`; для спільного використання призначений лише `.env.example`.

### 3. Перевірте конфігурацію та запустіть сервіси

З кореня репозиторію виконайте:

```powershell
docker compose config --quiet
docker compose up -d --build
```

Команда збирає образ API з наявного `Dockerfile` і запускає API, PostgreSQL та pgAdmin. API підключається до PostgreSQL за DNS-іменем сервісу `postgres` у внутрішній мережі Compose. Не використовуйте `localhost` як хост БД з контейнера API.

### 4. Перевірте стан контейнерів

```powershell
docker compose ps
docker compose logs --tail 100 api postgres pgadmin
```

PostgreSQL має перейти у стан `healthy`; API та pgAdmin мають бути `running`. Якщо сервіс не готовий, перегляньте його логи перед повторними спробами.

### 5. Відкрийте застосунки

- **Swagger UI:** [http://localhost:8080/swagger](http://localhost:8080/swagger)
- **pgAdmin:** [http://localhost:5050](http://localhost:5050)
- **PostgreSQL:** `localhost:5432` з хоста; порт `5432` у контейнерній мережі.

Для входу до pgAdmin використовуйте `PGADMIN_DEFAULT_EMAIL` і `PGADMIN_DEFAULT_PASSWORD` зі свого локального `.env`.

### Реєстрація PostgreSQL у pgAdmin

Після входу до pgAdmin:

1. Виберіть **Add New Server**.
2. На вкладці **General** задайте довільну назву, наприклад `EquipmentMonitoring`.
3. На вкладці **Connection** укажіть:
   - **Host name/address:** `postgres`
   - **Port:** `5432`
   - **Maintenance database:** значення `POSTGRES_DB` (типово `equipmentmonitoring`)
   - **Username:** значення `POSTGRES_USER`
   - **Password:** значення `POSTGRES_PASSWORD`
4. Увімкніть збереження пароля за потреби та збережіть сервер.

`postgres` — DNS-ім'я сервісу в мережі Docker Compose. Якщо під'єднуєтеся до PostgreSQL безпосередньо з хоста, використовуйте `localhost:5432`.

## Міграції та схема бази даних

Під час запуску API у PostgreSQL-середовищі застосовуються наявні EF Core міграції через `Database.MigrateAsync()`. Compose чекає на успішну перевірку готовності PostgreSQL перед запуском API. Для ручного запуску міграцій через EF Core використовуйте Infrastructure як проєкт і API як startup-проєкт, задавши `ConnectionStrings__EquipmentDatabase` для цільової БД.

Модель містить п'ять основних таблиць:

- `Equipments` — обладнання.
- `Measurements` — вимірювання, пов'язані з обладнанням.
- `Thresholds` — порогові значення для обладнання та типів вимірювань.
- `Alerts` — сповіщення, пов'язані з обладнанням і вимірюванням.
- `Maintenances` — записи технічного обслуговування.

Міграції створюють первинні й зовнішні ключі та індекси, зокрема унікальний індекс серійного номера обладнання. Не створюйте таблиці вручну паралельно з EF Core міграціями.

## Збереження даних і зупинка

Compose підключає іменований Docker Volume `equipmentmonitoring-postgres-data` до каталогу даних PostgreSQL. Дані зберігаються у Volume і мають переживати перезапуск або пересоздання контейнера PostgreSQL.

Безпечна тимчасова зупинка без видалення контейнерів і даних:

```powershell
docker compose stop
```

Щоб знову запустити зупинені сервіси:

```powershell
docker compose start
```

`docker compose down` зупиняє та прибирає контейнери й мережу, але іменований Volume зберігається, якщо не вказувати `-v`. **Не використовуйте `docker compose down -v`**, якщо потрібно зберегти базу даних. Перед будь-яким очищенням Docker-ресурсів переконайтеся, що потрібні дані мають резервну копію.

## Перевірка API

Swagger UI містить актуальні схеми та дозволяє виконати запити без додаткового клієнта. Основні маршрути:

- `POST /api/v1/equipment` — реєстрація обладнання.
- `POST /api/v1/measurements` — створення вимірювання.
- `GET /api/v1/equipment/{equipmentId}/measurements` — історія вимірювань з необов'язковими фільтрами `type`, `startDate` і `endDate` (межі дат включні).

### 1. Зареєструйте обладнання

У Swagger виконайте `POST /api/v1/equipment` із JSON тілом:

```json
{
  "name": "Temperature Sensor",
  "type": "Датчик",
  "serialNumber": "DOC-001-REPLACE-ME",
  "location": "Laboratory 1"
}
```

`serialNumber` має бути унікальним; замініть приклад на значення, якого ще немає в БД. Успішна відповідь `201 Created` містить `id` обладнання.

### 2. Додайте вимірювання

Передайте `id`, отриманий у відповіді попереднього запиту. Тип вимірювання — числове enum-значення: `0` — Temperature, `1` — Voltage, `2` — Load. `timestamp` необов'язковий; якщо його передаєте, використовуйте UTC.

```json
{
  "equipmentId": "PASTE-EQUIPMENT-GUID-HERE",
  "type": 0,
  "value": 23.5
}
```

Виконайте запит до `POST /api/v1/measurements`. Обладнання з таким ID має існувати.

### 3. Перегляньте історію

```http
GET /api/v1/equipment/PASTE-EQUIPMENT-GUID-HERE/measurements
```

Приклад із фільтрами:

```http
GET /api/v1/equipment/PASTE-EQUIPMENT-GUID-HERE/measurements?type=Temperature&startDate=2026-01-01T00:00:00Z&endDate=2026-12-31T23:59:59Z
```

У фактичному контракті використовуються `startDate` і `endDate`.

## Тести

Для запуску тестів із кореня репозиторію:

```powershell
dotnet test EquipmentMonitoring.slnx
```

Або відкрийте **Test → Test Explorer** у Visual Studio та запустіть потрібні тести. Результат залежить від поточного стану коду й середовища; перед змінами або демонстрацією запускайте тести повторно. Цей README не фіксує застарілий результат попереднього запуску.

## Командна робота

- `master` — основна гілка репозиторію.
- Для задач створюйте окремі feature-гілки від актуальної основної гілки.
- Інтегруйте зміни через Pull Request та перевіряйте збірку й тести.
- Не додавайте до Git локальний `.env`, паролі чи інші секрети.