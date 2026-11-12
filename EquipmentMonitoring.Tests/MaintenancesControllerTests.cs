using AutoMapper;
using EquipmentMonitoring.API.Controllers;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Mappings;
using EquipmentMonitoring.Application.Validators;
using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class MaintenancesControllerTests
{
    private EquipmentDbContext GetDbContext(bool seedEquipment = true)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new EquipmentDbContext(options);
        context.Database.EnsureCreated();

        if (seedEquipment)
        {
            var equipment = new Equipment("Тестове обладнання", "Модель A", "SN-12345", "Цех 1");
            context.Equipments.Add(equipment);
            context.SaveChanges();
        }

        return context;
    }

    private IMapper GetMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<MaintenanceMappingProfile>());
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task Create_ValidDto_Returns201Created()
    {
        var context = GetDbContext(seedEquipment: true);
        var controller = new MaintenancesController(
            context,
            GetMapper(),
            new CreateMaintenanceDtoValidator(),
            new UpdateMaintenanceDtoValidator());

        var dto = new CreateMaintenanceDto
        {
            EquipmentId = 1,
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            Description = "Планова діагностика"
        };

        var result = await controller.Create(dto);

        var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, actionResult.StatusCode);
    }

    [Fact]
    public async Task Create_NonExistingEquipment_Returns404NotFound()
    {
        var context = GetDbContext(seedEquipment: false);
        var controller = new MaintenancesController(
            context,
            GetMapper(),
            new CreateMaintenanceDtoValidator(),
            new UpdateMaintenanceDtoValidator());

        var dto = new CreateMaintenanceDto
        {
            EquipmentId = 999,
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            Description = "Тест"
        };

        var result = await controller.Create(dto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAll_ReturnsFilteredResults()
    {
        var context = GetDbContext(seedEquipment: true);

        var controller = new MaintenancesController(
            context,
            GetMapper(),
            new CreateMaintenanceDtoValidator(),
            new UpdateMaintenanceDtoValidator());

        var createDto = new CreateMaintenanceDto
        {
            EquipmentId = 1,
            ScheduledDate = DateTime.UtcNow,
            Description = "Тестове обслуговування"
        };
        await controller.Create(createDto);

        var result = await controller.GetAll(equipmentId: 1, status: MaintenanceStatus.Scheduled, fromDate: null, toDate: null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<MaintenanceResponseDto>>(okResult.Value);
        Assert.NotEmpty(items);
    }
}