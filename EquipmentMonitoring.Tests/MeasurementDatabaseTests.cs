using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class MeasurementDatabaseTests
{
    [Fact]
    public void Measurement_Should_Be_Registered_With_Id_As_Primary_Key()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var entityType = context.Model.FindEntityType(typeof(Measurement));

        Assert.NotNull(entityType);
        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(Measurement.Id), Assert.Single(primaryKey.Properties).Name);
    }

    [Fact]
    public void Measurement_Should_Have_Required_Equipment_Foreign_Key_And_Many_To_One_Relationship()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var measurementEntity = context.Model.FindEntityType(typeof(Measurement));
        Assert.NotNull(measurementEntity);

        var foreignKey = Assert.Single(measurementEntity.GetForeignKeys());
        Assert.Equal(typeof(Equipment), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(nameof(Measurement.EquipmentId), Assert.Single(foreignKey.Properties).Name);
        Assert.False(foreignKey.Properties[0].IsNullable);
        Assert.False(foreignKey.IsUnique);
    }

    [Fact]
    public void Measurement_Should_Have_Composite_Index_On_EquipmentId_And_Timestamp()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var measurementEntity = context.Model.FindEntityType(typeof(Measurement));
        Assert.NotNull(measurementEntity);

        var index = measurementEntity.GetIndexes()
            .SingleOrDefault(candidate => candidate.Properties
                .Select(property => property.Name)
                .SequenceEqual([nameof(Measurement.EquipmentId), nameof(Measurement.Timestamp)]));

        Assert.NotNull(index);
    }

    [Fact]
    public async Task Measurement_Should_Save_And_Load_All_Properties_Through_New_DbContext()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        var options = new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseSqlite(connection)
            .Options;
        var equipment = new Equipment("Sensor", "Датчик", "SN-MEASURE-001", "Lab 1");
        var beforeTimestamp = DateTime.UtcNow;
        var measurement = new Measurement(equipment.Id, MeasurementType.Temperature, -5.25);
        var afterTimestamp = DateTime.UtcNow;

        await using (var writeContext = new EquipmentDbContext(options))
        {
            await writeContext.Database.EnsureCreatedAsync(cancellationToken);
            writeContext.Add(equipment);
            writeContext.Add(measurement);
            await writeContext.SaveChangesAsync(cancellationToken);
        }

        await using var readContext = new EquipmentDbContext(options);
        var storedMeasurement = await readContext.Set<Measurement>()
            .AsNoTracking()
            .SingleAsync(item => item.Id == measurement.Id, cancellationToken);

        Assert.Equal(measurement.Id, storedMeasurement.Id);
        Assert.Equal(equipment.Id, storedMeasurement.EquipmentId);
        Assert.Equal(MeasurementType.Temperature, storedMeasurement.Type);
        Assert.Equal(-5.25, storedMeasurement.Value);
        Assert.Equal("°C", storedMeasurement.Unit);
        Assert.Equal(DateTimeKind.Utc, storedMeasurement.Timestamp.Kind);
        Assert.InRange(storedMeasurement.Timestamp, beforeTimestamp, afterTimestamp);
        Assert.Equal(measurement.Timestamp, storedMeasurement.Timestamp);
    }

    private static EquipmentDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseSqlite(connection)
            .Options;
        return new EquipmentDbContext(options);
    }
}
