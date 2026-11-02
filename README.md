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

## Поточний стан

Створено базову структуру рішення, налаштовано залежності між проєктами, підключено основні бібліотеки, перевірено запуск Web API та Swagger UI.

Тестове середовище xUnit налаштовано, початковий тест успішно виконано.

Реалізація функціональності системи, моделей даних, бази даних та основних API-методів запланована на наступні етапи розробки.

## Організація розробки

Розробка здійснюється командою з використанням Jira для керування завданнями та GitHub для контролю версій.

Для окремих завдань створюються робочі Git-гілки, а зміни інтегруються через Pull Requests.