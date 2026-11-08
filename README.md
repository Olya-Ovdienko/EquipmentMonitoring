# EquipmentMonitoring

## Опис проєкту

EquipmentMonitoring — навчальна система моніторингу обладнання, розроблена на платформі ASP.NET Core Web API з використанням C# та .NET 10.

Мета проєкту — реалізувати реєстрацію обладнання, збереження та перегляд його показників, контроль допустимих значень і планування технічного обслуговування.

Проєкт розробляється відповідно до вимог і декомпозиції завдань, створених у Jira.

## Архітектура проєкту

Рішення EquipmentMonitoring складається з п'яти проєктів:

**EquipmentMonitoring.Domain** — доменні моделі та основні сутності системи.

**EquipmentMonitoring.Application** — бізнес-логіка, DTO, інтерфейси та валідація даних.

**EquipmentMonitoring.Infrastructure** — доступ до бази даних, Entity Framework Core та репозиторії.

**EquipmentMonitoring.API** — HTTP API, контролери та документація Swagger.

**EquipmentMonitoring.Tests** — модульне тестування з використанням xUnit і методології TDD.

## Використані технології

.NET 10, ASP.NET Core Web API, C#, Entity Framework Core, SQLite, FluentValidation, Swagger / OpenAPI, xUnit v3, Git, GitHub та GitHub Copilot.

## Запуск проєкту

Для запуску необхідно встановити Visual Studio з підтримкою ASP.NET Core та .NET 10 SDK.

Відкрити рішення EquipmentMonitoring.slnx у Visual Studio.

Встановити EquipmentMonitoring.API як Startup Project (проєкт запуску).

Запустити застосунок за допомогою Ctrl + F5.

Відкрити Swagger UI за адресою https://localhost:54960/swagger або за адресою, вказаною в налаштуваннях запуску.

## Тестування

Модульні тести виконуються в EquipmentMonitoring.Tests.

Для запуску тестів у Visual Studio використовується Test → Test Explorer → Run All Tests.

Розробка функціональності передбачає застосування циклу TDD: Red → Green → Refactor.

## TDD — EPT-8: Модель Equipment

**RED** — створено модульний тест Equipment_Should_Generate_NonEmpty_Id для перевірки автоматичної генерації унікального ідентифікатора обладнання. Початкова збірка завершилася помилками через відсутність реалізації класу Equipment.

**GREEN** — реалізовано клас Equipment із властивостями Id, Name, Type, SerialNumber та Location. Додано автоматичну генерацію Id через Guid.NewGuid(). Збірка успішна, обидва тести xUnit пройдено.

**REFACTOR** — за допомогою GitHub Copilot проведено аналіз якості реалізації Equipment. Код має просту та зрозумілу структуру, тому додаткові зміни не знадобилися. Повторна збірка успішна, 2 з 2 тестів пройдено.

Етапи RED і GREEN зафіксовано окремими Git-комітами. Результати перевірки REFACTOR задокументовано.

### Цикл 2 — Status і CreatedAt

**RED** — додано три тести для EquipmentStatus, Status і CreatedAt. Збірка не пройшла через відсутність нових типів і властивостей.

**GREEN** — створено EquipmentStatus із чотирма станами: Available, InUse, Maintenance, Disabled. До Equipment додано Status і CreatedAt з автоматичним встановленням часу UTC. Усі 5 тестів пройдено.

**REFACTOR** — GitHub Copilot перевірив структуру та якість коду. Змін не знадобилося. Збірка успішна, 5 із 5 тестів пройдено.

### Цикл 3 — Валідація Name

**RED** — додано тести для перевірки обов'язковості Name та максимальної довжини 100 символів. Збірка успішна, 4 тестові випадки не пройшли.

**GREEN** — реалізовано перевірки Name через string.IsNullOrWhiteSpace(name) та name.Length > 100. Некоректні значення відхиляються через ArgumentException. Усі 10 тестів пройдено.

**REFACTOR** — GitHub Copilot перевірив якість і структуру коду. Зміни не знадобилися, поведінку моделі збережено. Збірка успішна, 10 із 10 тестів пройдено.

### Цикл 4 — EF Core та унікальність SerialNumber

**RED** — створено три тести для перевірки конфігурації EF Core, обмежень Name та унікальності SerialNumber у SQLite. Збірка не пройшла через відсутність EquipmentDbContext.

**GREEN** — реалізовано EquipmentDbContext і EquipmentConfiguration. Налаштовано первинний ключ Id, обов'язковість і максимальну довжину Name, унікальний індекс SerialNumber. Усі 13 тестів пройдено.

**REFACTOR** — GitHub Copilot перевірив конфігурацію EF Core та відображення властивостей Equipment. Змістовні зміни не знадобилися. Збірка успішна, 13 із 13 тестів пройдено.

## Поточний стан

Реалізовано доменну модель Equipment зі станом і UTC-часом створення, EF Core persistence на SQLite з міграцією та унікальним індексом SerialNumber, DTO та FluentValidation.

Реалізовано реєстрацію обладнання через POST /api/v1/equipment, асинхронні AddAsync і GetByIdAsync репозиторію та обробку HTTP-відповідей 201, 400 і 409. Додано модульні, інтеграційні та HTTP-тести на xUnit v3.

Інші функції моніторингу показників і планування технічного обслуговування поки не описуються як реалізовані.

## Документація API

Swagger UI доступний під час локального запуску в середовищі Development за адресою https://localhost:54960/swagger (або за адресою, вказаною у профілі запуску Visual Studio).

### POST /api/v1/equipment

Реєструє обладнання після перевірки обов'язкових полів, довжини Name, допустимого Type та унікальності SerialNumber.

Приклад JSON-запиту:

```json
{
  "name": "Temperature Sensor",
  "type": "Датчик",
  "serialNumber": "DOC-001",
  "location": "Laboratory 1"
}
```

- **201 Created** — обладнання збережено; повертається EquipmentResponseDto із серверним Id, початковим Status і CreatedAt у UTC.
- **400 Bad Request** — помилки валідації, згруповані за полями у ValidationProblemDetails.
- **409 Conflict** — SerialNumber уже використовується.

Щоб перевірити запит у Swagger, відкрийте Swagger UI, розгорніть `POST /api/v1/equipment`, натисніть **Try it out**, задайте JSON і натисніть **Execute**. Приклади запиту та відповідей наведено в документації операції.

## Організація розробки

Розробка здійснюється командою з використанням Jira для керування завданнями та GitHub для контролю версій.

Для окремих завдань створюються робочі Git-гілки, а зміни інтегруються через Pull Requests.