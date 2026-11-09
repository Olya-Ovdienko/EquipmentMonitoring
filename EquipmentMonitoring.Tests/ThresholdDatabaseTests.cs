using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class ThresholdDatabaseTests
{
    [Fact]
    public void Threshold_Should_Be_Registered_In_DbContext()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var entityType = context.Model.FindEntityType(typeof(Threshold));

        Assert.NotNull(entityType);
    }

    [Fact]
    public void Threshold_Should_Use_Id_As_Application_Generated_Primary_Key()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var entityType = context.Model.FindEntityType(typeof(Threshold));
        Assert.NotNull(entityType);
        var primaryKey = entityType!.FindPrimaryKey();
        var idProperty = entityType!.FindProperty(nameof(Threshold.Id));

        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(Threshold.Id), Assert.Single(primaryKey.Properties).Name);
        Assert.NotNull(idProperty);
        Assert.Equal(ValueGenerated.Never, idProperty.ValueGenerated);
    }

    [Fact]
    public void Threshold_Should_Have_Nullable_MinValue_And_MaxValue()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var entityType = context.Model.FindEntityType(typeof(Threshold));
        Assert.NotNull(entityType);
        var minValue = entityType!.FindProperty(nameof(Threshold.MinValue));
        var maxValue = entityType!.FindProperty(nameof(Threshold.MaxValue));
        Assert.NotNull(minValue);
        Assert.NotNull(maxValue);

        Assert.True(minValue!.IsNullable);
        Assert.True(maxValue!.IsNullable);
    }

    [Fact]
    public void Threshold_Should_Have_Required_Equipment_Foreign_Key()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var entityType = context.Model.FindEntityType(typeof(Threshold));
        Assert.NotNull(entityType);
        var foreignKey = Assert.Single(entityType!.GetForeignKeys());

        Assert.Equal(typeof(Equipment), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(nameof(Threshold.EquipmentId), Assert.Single(foreignKey.Properties).Name);
        Assert.False(foreignKey.Properties[0].IsNullable);
        Assert.False(foreignKey.IsUnique);
    }

    [Fact]
    public void Threshold_Should_Have_Index_On_EquipmentId_MeasurementType_And_Severity()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var entityType = context.Model.FindEntityType(typeof(Threshold));
        Assert.NotNull(entityType);
        var index = entityType!.GetIndexes().SingleOrDefault(candidate => candidate.Properties
            .Select(property => property.Name)
            .SequenceEqual([
                nameof(Threshold.EquipmentId),
                nameof(Threshold.MeasurementType),
                nameof(Threshold.Severity)]));

        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }

    [Fact]
    public void Threshold_Should_Reject_Duplicate_EquipmentId_MeasurementType_And_Severity()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);
        context.Database.EnsureCreated();

        var equipment = new Equipment("Sensor", "Датчик", "SN-THRESHOLD-002", "Lab 1");
        var firstThreshold = new Threshold(
            equipment.Id,
            MeasurementType.Temperature,
            10.0,
            90.0,
            AlertSeverity.Warning);
        var duplicateThreshold = new Threshold(
            equipment.Id,
            MeasurementType.Temperature,
            15.0,
            85.0,
            AlertSeverity.Warning);

        context.Add(equipment);
        context.Add(firstThreshold);
        context.SaveChanges();

        context.Add(duplicateThreshold);

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void Threshold_Should_Save_And_Load_All_Properties_Through_Sqlite()
    {
        using var connection = CreateConnection();
        using var writeContext = CreateContext(connection);
        writeContext.Database.EnsureCreated();

        var equipment = new Equipment("Sensor", "Датчик", "SN-THRESHOLD-001", "Lab 1");
        var threshold = new Threshold(
            equipment.Id,
            MeasurementType.Temperature,
            10.5,
            null,
            AlertSeverity.Warning);

        writeContext.Add(equipment);
        writeContext.Add(threshold);
        writeContext.SaveChanges();

        using var readContext = CreateContext(connection);
        var storedThreshold = readContext.Set<Threshold>()
            .AsNoTracking()
            .Single(item => item.Id == threshold.Id);

        Assert.Equal(threshold.Id, storedThreshold.Id);
        Assert.Equal(equipment.Id, storedThreshold.EquipmentId);
        Assert.Equal(MeasurementType.Temperature, storedThreshold.MeasurementType);
        Assert.Equal(10.5, storedThreshold.MinValue);
        Assert.Null(storedThreshold.MaxValue);
        Assert.Equal(AlertSeverity.Warning, storedThreshold.Severity);
    }

    [Fact]
    public void Threshold_Should_Reject_Missing_Equipment_Foreign_Key()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);
        context.Database.EnsureCreated();

        context.Add(new Threshold(
            Guid.NewGuid(),
            MeasurementType.Temperature,
            10.0,
            90.0,
            AlertSeverity.Warning));

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void Threshold_Equipment_Foreign_Key_Should_Use_Restrict_Or_NoAction_Delete_Behavior()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var entityType = context.Model.FindEntityType(typeof(Threshold));
        Assert.NotNull(entityType);
        var foreignKey = Assert.Single(entityType!.GetForeignKeys());

        Assert.True(
            foreignKey.DeleteBehavior is DeleteBehavior.Restrict or DeleteBehavior.NoAction);
    }

    private static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();
        return connection;
    }

    private static EquipmentDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseSqlite(connection)
            .Options;
        return new EquipmentDbContext(options);
    }
}
