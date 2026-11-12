using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class MaintenanceRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly EquipmentDbContext _context;
    private readonly MaintenanceRepository _repository;

    public MaintenanceRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new EquipmentDbContext(options);
        _context.Database.EnsureCreated();

        _repository = new MaintenanceRepository(_context);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistMaintenanceWithEquipmentRelation()
    {
        // Arrange
        var equipment = new Equipment("Генератор", "Model X", "SN-999", "Цех 1");
        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync();

        var maintenance = new Maintenance
        {
            EquipmentId = equipment.Id,
            ScheduledDate = DateTime.UtcNow.AddDays(2),
            Description = "Заміна фільтрів",
            Status = MaintenanceStatus.Scheduled
        };

        // Act
        await _repository.CreateAsync(maintenance);

        // Assert
        var result = await _repository.GetByIdAsync(maintenance.Id);
        Assert.NotNull(result);
        Assert.Equal(equipment.Id, result.EquipmentId);
        Assert.Equal("Заміна фільтрів", result.Description);
        Assert.Equal(MaintenanceStatus.Scheduled, result.Status);
    }

    [Fact]
    public async Task GetByEquipmentIdAsync_ShouldReturnOnlyMatchingMaintenances()
    {
        // Arrange
        var equipment1 = new Equipment("Двигун 1", "M1", "SN-1", "Локація 1");
        var equipment2 = new Equipment("Двигун 2", "M2", "SN-2", "Локація 2");
        _context.Equipments.AddRange(equipment1, equipment2);
        await _context.SaveChangesAsync();

        var m1 = new Maintenance { EquipmentId = equipment1.Id, ScheduledDate = DateTime.UtcNow, Description = "ТО 1", Status = MaintenanceStatus.Scheduled };
        var m2 = new Maintenance { EquipmentId = equipment2.Id, ScheduledDate = DateTime.UtcNow, Description = "ТО 2", Status = MaintenanceStatus.Scheduled };

        _context.Maintenances.AddRange(m1, m2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEquipmentIdAsync(equipment1.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal(equipment1.Id, result[0].EquipmentId);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdatePerformedDateDescriptionAndStatus()
    {
        // Arrange
        var equipment = new Equipment("Насос", "P100", "SN-55", "Склад");
        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync();

        var maintenance = new Maintenance
        {
            EquipmentId = equipment.Id,
            ScheduledDate = DateTime.UtcNow.AddDays(-1),
            Description = "Планове ТО",
            Status = MaintenanceStatus.Scheduled
        };
        await _repository.CreateAsync(maintenance);

        // Act
        maintenance.Status = MaintenanceStatus.Completed;
        maintenance.PerformedDate = DateTime.UtcNow;
        maintenance.Description = "Планове ТО успішно виконано";
        await _repository.UpdateAsync(maintenance);

        // Assert
        var updated = await _repository.GetByIdAsync(maintenance.Id);
        Assert.NotNull(updated);
        Assert.Equal(MaintenanceStatus.Completed, updated.Status);
        Assert.NotNull(updated.PerformedDate);
        Assert.Equal("Планове ТО успішно виконано", updated.Description);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(99999);

        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}