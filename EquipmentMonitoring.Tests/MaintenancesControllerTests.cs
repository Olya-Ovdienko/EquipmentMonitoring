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
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class MaintenancesControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly EquipmentDbContext _context;
    private readonly IMapper _mapper;
    private readonly MaintenancesController _controller;

    public MaintenancesControllerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new EquipmentDbContext(options);
        _context.Database.EnsureCreated();

        var config = new MapperConfiguration(
            cfg => cfg.AddProfile(new MaintenanceMappingProfile()),
            NullLoggerFactory.Instance);

        _mapper = config.CreateMapper();

        var createValidator = new CreateMaintenanceDtoValidator();
        var updateValidator = new UpdateMaintenanceDtoValidator();

        _controller = new MaintenancesController(_context, _mapper, createValidator, updateValidator);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var equipment = new Equipment("Тест", "Model", "SN123", "Location");
        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync(CancellationToken.None);

        var maintenance = new Maintenance
        {
            EquipmentId = equipment.Id,
            Equipment = equipment,
            ScheduledDate = DateTime.UtcNow,
            Description = "Тест",
            Status = MaintenanceStatus.Scheduled
        };
        _context.Maintenances.Add(maintenance);
        await _context.SaveChangesAsync(CancellationToken.None);

        var result = await _controller.GetAll(cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<MaintenanceResponseDto>>(okResult.Value);
        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var equipment = new Equipment("Тест", "Model", "SN123", "Location");
        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync(CancellationToken.None);

        var maintenance = new Maintenance
        {
            EquipmentId = equipment.Id,
            Equipment = equipment,
            ScheduledDate = DateTime.UtcNow,
            Description = "Тест",
            Status = MaintenanceStatus.Scheduled
        };
        _context.Maintenances.Add(maintenance);
        await _context.SaveChangesAsync(CancellationToken.None);

        var result = await _controller.GetById(maintenance.Id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        var equipment = new Equipment("Тест", "Model", "SN123", "Location");
        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync(CancellationToken.None);

        var dto = new CreateMaintenanceDto
        {
            EquipmentId = equipment.Id,
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            Description = "Тест опис"
        };

        var result = await _controller.Create(dto, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(createdResult.Value);
    }

    [Fact]
    public async Task GetAll_WithFilters_ReturnsFilteredList()
    {
        var equipment = new Equipment("Тест", "Model", "SN123", "Location");
        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync(CancellationToken.None);

        var maintenance = new Maintenance
        {
            EquipmentId = equipment.Id,
            Equipment = equipment,
            ScheduledDate = DateTime.UtcNow,
            Description = "Тест",
            Status = MaintenanceStatus.Scheduled
        };
        _context.Maintenances.Add(maintenance);
        await _context.SaveChangesAsync(CancellationToken.None);

        var result = await _controller.GetAll(
            equipmentId: equipment.Id,
            status: MaintenanceStatus.Scheduled,
            fromDate: DateTime.UtcNow.AddDays(-1),
            toDate: DateTime.UtcNow.AddDays(1),
            cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<MaintenanceResponseDto>>(okResult.Value);
        Assert.Single(items);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}