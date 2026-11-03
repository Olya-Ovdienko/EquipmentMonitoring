using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class EquipmentDatabaseTests
{
    [Fact]
    public void Equipment_Name_Should_Be_Required_And_Have_Maximum_Length_100()
    {
        var options = new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var context = new EquipmentDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(Equipment));
        Assert.NotNull(entityType);

        var nameProperty = entityType.FindProperty(nameof(Equipment.Name));
        Assert.NotNull(nameProperty);
        Assert.False(nameProperty.IsNullable);
        Assert.Equal(100, nameProperty.GetMaxLength());
    }

    [Fact]
    public void Equipment_SerialNumber_Should_Have_Unique_Index()
    {
        var options = new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var context = new EquipmentDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(Equipment));
        Assert.NotNull(entityType);

        var serialNumberProperty = entityType.FindProperty(nameof(Equipment.SerialNumber));
        Assert.NotNull(serialNumberProperty);

        var index = entityType.GetIndexes()
            .SingleOrDefault(candidate => candidate.Properties.Contains(serialNumberProperty));

        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }

    [Fact]
    public async Task Equipment_Should_Not_Save_Duplicate_SerialNumbers()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var options = new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new EquipmentDbContext(options);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        context.Set<Equipment>().Add(new Equipment("Laptop", "Computer", "SN-001", "Office 1"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Set<Equipment>().Add(new Equipment("Monitor", "Display", "SN-001", "Office 2"));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Equipment_Should_Save_And_Load_All_Properties()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);

        var options = new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseSqlite(connection)
            .Options;
        var equipment = new Equipment("Laptop", "Computer", "SN-100", "Office 1");

        await using (var writeContext = new EquipmentDbContext(options))
        {
            await writeContext.Database.EnsureCreatedAsync(cancellationToken);
            writeContext.Equipments.Add(equipment);
            await writeContext.SaveChangesAsync(cancellationToken);
        }

        await using var readContext = new EquipmentDbContext(options);
        var loadedEquipment = await readContext.Equipments
            .AsNoTracking()
            .SingleAsync(item => item.Id == equipment.Id, cancellationToken);

        Assert.Equal(equipment.Id, loadedEquipment.Id);
        Assert.Equal(equipment.Name, loadedEquipment.Name);
        Assert.Equal(equipment.Type, loadedEquipment.Type);
        Assert.Equal(equipment.SerialNumber, loadedEquipment.SerialNumber);
        Assert.Equal(equipment.Location, loadedEquipment.Location);
        Assert.Equal(equipment.Status, loadedEquipment.Status);
        Assert.Equal(equipment.CreatedAt, loadedEquipment.CreatedAt);
    }
}
