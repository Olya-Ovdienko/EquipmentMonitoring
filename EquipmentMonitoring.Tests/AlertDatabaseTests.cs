using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class AlertDatabaseTests
{
    [Fact]
    public void Alert_Should_Be_Registered_In_DbContext()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var entityType = context.Model.FindEntityType(typeof(Alert));

        Assert.NotNull(entityType);
    }

    [Fact]
    public void Alert_Should_Use_Id_As_Application_Generated_Primary_Key()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var entityType = context.Model.FindEntityType(typeof(Alert));
        Assert.NotNull(entityType);
        var primaryKey = entityType!.FindPrimaryKey();
        var idProperty = entityType!.FindProperty(nameof(Alert.Id));

        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(Alert.Id), Assert.Single(primaryKey.Properties).Name);
        Assert.NotNull(idProperty);
        Assert.Equal(ValueGenerated.Never, idProperty.ValueGenerated);
    }

    [Fact]
    public void Alert_Should_Have_Required_Equipment_Foreign_Key()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var entityType = context.Model.FindEntityType(typeof(Alert));
        Assert.NotNull(entityType);
        var foreignKey = entityType!.GetForeignKeys()
            .Single(candidate => candidate.Properties.Single().Name == nameof(Alert.EquipmentId));

        Assert.Equal(typeof(Equipment), foreignKey.PrincipalEntityType.ClrType);
        Assert.False(foreignKey.Properties[0].IsNullable);
        Assert.False(foreignKey.IsUnique);
    }

    [Fact]
    public void Alert_Should_Have_Required_Measurement_Foreign_Key()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var entityType = context.Model.FindEntityType(typeof(Alert));
        Assert.NotNull(entityType);
        var foreignKey = entityType!.GetForeignKeys()
            .Single(candidate => candidate.Properties.Single().Name == nameof(Alert.MeasurementId));

        Assert.Equal(typeof(Measurement), foreignKey.PrincipalEntityType.ClrType);
        Assert.False(foreignKey.Properties[0].IsNullable);
        Assert.False(foreignKey.IsUnique);
    }

    [Fact]
    public void Alert_Should_Have_Index_On_EquipmentId_And_Timestamp()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var entityType = context.Model.FindEntityType(typeof(Alert));
        Assert.NotNull(entityType);
        var index = entityType!.GetIndexes().SingleOrDefault(candidate => candidate.Properties
            .Select(property => property.Name)
            .SequenceEqual([nameof(Alert.EquipmentId), nameof(Alert.Timestamp)]));

        Assert.NotNull(index);
    }

    [Fact]
    public void Alert_Should_Have_Index_On_EquipmentId_And_IsAcknowledged()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var entityType = context.Model.FindEntityType(typeof(Alert));
        Assert.NotNull(entityType);
        var index = entityType!.GetIndexes().SingleOrDefault(candidate => candidate.Properties
            .Select(property => property.Name)
            .SequenceEqual([nameof(Alert.EquipmentId), nameof(Alert.IsAcknowledged)]));

        Assert.NotNull(index);
    }

    [Fact]
    public void Alert_Should_Save_And_Load_All_Properties_Through_Sqlite()
    {
        using var connection = CreateConnection();
        using var writeContext = CreateContext(connection);
        writeContext.Database.EnsureCreated();

        var equipment = new Equipment("Sensor", "Датчик", "SN-ALERT-001", "Lab 1");
        var measurement = new Measurement(
            equipment.Id,
            MeasurementType.Temperature,
            95.0,
            DateTime.UtcNow);
        var alert = new Alert(
            equipment.Id,
            measurement.Id,
            "Temperature exceeded the allowed threshold.",
            AlertSeverity.Critical,
            measurement.Timestamp);

        writeContext.Add(equipment);
        writeContext.Add(measurement);
        writeContext.Add(alert);
        writeContext.SaveChanges();

        using var readContext = CreateContext(connection);
        var storedAlert = readContext.Set<Alert>()
            .AsNoTracking()
            .Single(item => item.Id == alert.Id);

        Assert.Equal(alert.Id, storedAlert.Id);
        Assert.Equal(equipment.Id, storedAlert.EquipmentId);
        Assert.Equal(measurement.Id, storedAlert.MeasurementId);
        Assert.Equal(alert.Message, storedAlert.Message);
        Assert.Equal(AlertSeverity.Critical, storedAlert.Severity);
        Assert.Equal(alert.Timestamp, storedAlert.Timestamp);
        Assert.False(storedAlert.IsAcknowledged);
    }

    [Fact]
    public void Alert_Should_Reject_Missing_Equipment_Foreign_Key()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);
        context.Database.EnsureCreated();

        var equipment = new Equipment("Sensor", "Датчик", "SN-ALERT-002", "Lab 1");
        var measurement = new Measurement(equipment.Id, MeasurementType.Temperature, 95.0);
        context.Add(equipment);
        context.Add(measurement);
        context.SaveChanges();

        var missingEquipmentId = Guid.NewGuid();
        context.Add(new Alert(
            missingEquipmentId,
            measurement.Id,
            "Temperature exceeded the allowed threshold.",
            AlertSeverity.Warning,
            measurement.Timestamp));

        var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
        var sqliteException = Assert.IsType<SqliteException>(exception.InnerException);

        Assert.Equal(787, sqliteException.SqliteExtendedErrorCode);
        Assert.Contains("FOREIGN KEY constraint failed", sqliteException.Message);
    }

    [Fact]
    public void Alert_Should_Reject_Missing_Measurement_Foreign_Key()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);
        context.Database.EnsureCreated();

        var equipment = new Equipment("Sensor", "Датчик", "SN-ALERT-003", "Lab 1");
        context.Add(equipment);
        context.Add(new Alert(
            equipment.Id,
            Guid.NewGuid(),
            "Temperature exceeded the allowed threshold.",
            AlertSeverity.Warning,
            DateTime.UtcNow));

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void Alert_Equipment_Foreign_Key_Should_Use_Restrict_Or_NoAction_Delete_Behavior()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var entityType = context.Model.FindEntityType(typeof(Alert));
        Assert.NotNull(entityType);
        var foreignKey = entityType!.GetForeignKeys()
            .Single(candidate => candidate.Properties.Single().Name == nameof(Alert.EquipmentId));

        Assert.True(
            foreignKey.DeleteBehavior is DeleteBehavior.Restrict or DeleteBehavior.NoAction);
    }

    [Fact]
    public void Alert_Measurement_Foreign_Key_Should_Use_Restrict_Or_NoAction_Delete_Behavior()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);

        var entityType = context.Model.FindEntityType(typeof(Alert));
        Assert.NotNull(entityType);
        var foreignKey = entityType!.GetForeignKeys()
            .Single(candidate => candidate.Properties.Single().Name == nameof(Alert.MeasurementId));

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
