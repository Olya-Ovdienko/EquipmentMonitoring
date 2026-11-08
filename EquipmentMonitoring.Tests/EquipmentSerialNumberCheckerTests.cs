using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class EquipmentSerialNumberCheckerTests
{
    [Fact]
    public async Task IsSerialNumberUniqueAsync_Should_Return_True_For_New_Number()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        await using var context = await CreateDbContextAsync(connection, cancellationToken);
        var checker = new EquipmentSerialNumberChecker(context);

        var isUnique = await checker.IsSerialNumberUniqueAsync("SN-NEW", null, cancellationToken);

        Assert.True(isUnique);
    }

    [Fact]
    public async Task IsSerialNumberUniqueAsync_Should_Return_False_For_Existing_Number_On_Create()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        await using var context = await CreateDbContextAsync(connection, cancellationToken);
        var equipment = new Equipment("Датчик", "Датчик", "SN-001", "Лабораторія");
        context.Equipments.Add(equipment);
        await context.SaveChangesAsync(cancellationToken);
        var checker = new EquipmentSerialNumberChecker(context);

        var isUnique = await checker.IsSerialNumberUniqueAsync(equipment.SerialNumber, null, cancellationToken);

        Assert.False(isUnique);
    }

    [Fact]
    public async Task IsSerialNumberUniqueAsync_Should_Return_True_When_Excluding_Current_Equipment()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        await using var context = await CreateDbContextAsync(connection, cancellationToken);
        var equipment = new Equipment("Датчик", "Датчик", "SN-001", "Лабораторія");
        context.Equipments.Add(equipment);
        await context.SaveChangesAsync(cancellationToken);
        var checker = new EquipmentSerialNumberChecker(context);

        var isUnique = await checker.IsSerialNumberUniqueAsync(
            equipment.SerialNumber,
            equipment.Id,
            cancellationToken);

        Assert.True(isUnique);
    }

    [Fact]
    public async Task IsSerialNumberUniqueAsync_Should_Return_False_When_Excluding_Other_Equipment()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        await using var context = await CreateDbContextAsync(connection, cancellationToken);
        var equipment = new Equipment("Датчик", "Датчик", "SN-001", "Лабораторія");
        context.Equipments.Add(equipment);
        await context.SaveChangesAsync(cancellationToken);
        var checker = new EquipmentSerialNumberChecker(context);
        var otherEquipmentId = Guid.NewGuid();

        var isUnique = await checker.IsSerialNumberUniqueAsync(
            equipment.SerialNumber,
            otherEquipmentId,
            cancellationToken);

        Assert.False(isUnique);
    }

    private static async Task<EquipmentDbContext> CreateDbContextAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new EquipmentDbContext(options);
        await context.Database.EnsureCreatedAsync(cancellationToken);

        return context;
    }
}
