# EquipmentMonitoring

## Про проєкт

EquipmentMonitoring — навчальний ASP.NET Core Web API для реєстрації обладнання та зберігання і перегляду його вимірювань. Рішення орієнтоване на .NET 10, SQLite та Entity Framework Core.

### Статус функціональності

- **EPT-4 — реєстрація обладнання:** реалізовано. Доступні створення обладнання, валідація полів і унікальності серійного номера.
- **EPT-5 — додавання та перегляд вимірювань:** реалізовано. Доступні створення вимірювань і перегляд історії з фільтрами за типом та діапазоном дат.
- **EPT-6 — контроль перевищень і попередження:** ще не реалізовано.
- **EPT-7 — планування технічного обслуговування:** ще не реалізовано.

## Структура рішення

`EquipmentMonitoring.slnx` містить п'ять проєктів:

- **EquipmentMonitoring.Domain** — доменні сутності та типи.
- **EquipmentMonitoring.Application** — DTO, інтерфейси, сервіси та FluentValidation.
- **EquipmentMonitoring.Infrastructure** — EF Core, SQLite, конфігурація DbContext, міграції та репозиторії.
- **EquipmentMonitoring.API** — ASP.NET Core контролери, API та Swagger/OpenAPI.
- **EquipmentMonitoring.Tests** — тести xUnit v3, включно з модульними й інтеграційними тестами.

## Вимоги

- **.NET 10 SDK** (для розробки та запуску потрібна актуальна версія SDK гілки .NET 10; проєкти цільово використовують `net10.0`).
- Visual Studio 2026 з підтримкою ASP.NET Core або інше середовище розробки .NET.
- Для створення БД через Package Manager Console у Visual Studio доступні EF Core Tools 10.0.14; пакет інструментів налаштований у `EquipmentMonitoring.Infrastructure`.

## Початок роботи

1. Клонуйте репозиторій і перейдіть до його каталогу:

   ```powershell
   git clone https://github.com/Olya-Ovdienko/EquipmentMonitoring.git
   cd EquipmentMonitoring
   ```

2. Встановіть .NET 10 SDK і перевірте його наявність:

   ```powershell
   dotnet --list-sdks
   ```

   У списку має бути SDK версії `10.x`.

3. Відкрийте `EquipmentMonitoring.slnx` у Visual Studio (File → Open → Project/Solution). Для запуску виберіть `EquipmentMonitoring.API` як Startup Project.

4. Відновіть NuGet-пакети з кореня репозиторію:

   ```powershell
   dotnet restore EquipmentMonitoring.slnx
   ```

## Локальна SQLite-база даних

База створюється **окремо на кожному комп'ютері** з наявних EF Core міграцій. У репозиторії є міграції `InitialEquipment` і `AddMeasurements`. `EquipmentDbContextFactory` налаштовує SQLite-файл за шляхом `EquipmentMonitoring.API\equipmentmonitoring.db`; цей файл та SQLite journal-файли ігноруються Git, тому локальні дані не передаються іншим учасникам.

### Створення бази у Visual Studio

1. Відкрийте **Tools → NuGet Package Manager → Package Manager Console**.
2. У списку **Default project** виберіть `EquipmentMonitoring.Infrastructure`.
3. Переконайтеся, що **Startup Project** рішення — `EquipmentMonitoring.API`.
4. Виконайте команду:

   ```powershell
   Update-Database -Project EquipmentMonitoring.Infrastructure -StartupProject EquipmentMonitoring.API -Context EquipmentDbContext
   ```

Це використовує наявний `EquipmentDbContextFactory` із проєкту Infrastructure і застосовує міграції до `EquipmentMonitoring.API\equipmentmonitoring.db`. Під час першого запуску команди EF Core створить файл БД і таблиці. Не потрібно створювати таблиці вручну або використовувати `EnsureCreated` замість міграцій.

## Запуск API та Swagger

Запустіть `EquipmentMonitoring.API` у Visual Studio через **Ctrl+F5** або з кореня репозиторію командою:

```powershell
dotnet run --project EquipmentMonitoring.API
```

Профіль запуску Visual Studio використовує середовище `Development` і адреси `https://localhost:54960` та `http://localhost:54961`. Swagger увімкнений у Development:

- Swagger UI: `https://localhost:54960/swagger` (або `http://localhost:54961/swagger`)
- OpenAPI JSON: `https://localhost:54960/swagger/v1/swagger.json` (або `http://localhost:54961/swagger/v1/swagger.json`)

Якщо запускаєте API на іншій адресі або порті, використовуйте відповідну адресу для Swagger.

## Перевірка основного сценарію через Swagger

1. Відкрийте Swagger UI та виконайте `POST /api/v1/equipment`, створивши обладнання, наприклад:

   ```json
   {
     "name": "Temperature Sensor",
     "type": "Датчик",
     "serialNumber": "DOC-001",
     "location": "Laboratory 1"
   }
   ```

2. У відповіді `201 Created` скопіюйте `id` створеного обладнання.
3. Виконайте `POST /api/v1/measurements`. У полі `equipmentId` передайте цей `id`; вкажіть підтримуваний `type` (числове enum-значення: `0` — Temperature, `1` — Voltage, `2` — Load) і виміряне `value`. `timestamp` необов'язковий.
4. Виконайте `GET /api/v1/equipment/{equipmentId}/measurements`, підставивши той самий Id. За потреби додайте query-фільтри `type`, `startDate` та `endDate`. Межі дат включні.

> У Jira для фільтрів EPT-21 зазначені `fromDate`/`toDate`, але фактичний контракт API використовує `startDate`/`endDate`. У запитах використовуйте реальні параметри `startDate` і `endDate`.

## Тести

Запустити всі тести з кореня репозиторію:

```powershell
dotnet test EquipmentMonitoring.slnx
```

Або у Visual Studio відкрийте **Test → Test Explorer → Run All Tests**. Останній відомий результат команди: **208 Passed, 0 Failed**. Перед Pull Request повторно виконуйте збірку рішення та всі тести.

## Командна робота

- `master` — основна гілка.
- Для кожної задачі створюйте окрему feature-гілку від актуального `master`, наприклад `feature/EPT-6-alerts` або `feature/EPT-7-maintenance`.
- Прив'язуйте назви комітів і повідомлення до ключа Jira (наприклад, `EPT-6: ...`).
- Інтегруйте зміни через Pull Request; перед злиттям перевіряйте збірку та всі тести.
- Не змінюйте чужі функціональні модулі без необхідності; узгоджуйте зміни спільних контрактів із командою.
- Не додавайте до Git локальні SQLite-файли, секрети або персональні налаштування середовища.

## Основні API-маршрути

- `POST /api/v1/equipment` — реєстрація обладнання.
- `POST /api/v1/measurements` — створення вимірювання.
- `GET /api/v1/equipment/{equipmentId}/measurements` — історія вимірювань обладнання з необов'язковими фільтрами `type`, `startDate` і `endDate`.