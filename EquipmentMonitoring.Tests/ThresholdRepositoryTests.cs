using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class ThresholdRepositoryTests
{
    [Fact]
    public async Task GetByEquipmentAndMeasurementTypeAsync_Should_Return_Matching_Equipment_And_Type()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var equipment = CreateEquipment();
        var otherEquipment = CreateEquipment();
        await SeedAsync(database, cancellationToken, equipment, otherEquipment);
        await SeedThresholdsAsync(
            database,
            cancellationToken,
            new Threshold(equipment.Id, MeasurementType.Temperature, 10.0, 90.0, AlertSeverity.Warning),
            new Threshold(otherEquipment.Id, MeasurementType.Temperature, 10.0, 90.0, AlertSeverity.Warning),
            new Threshold(equipment.Id, MeasurementType.Voltage, 110.0, 240.0, AlertSeverity.Warning));

        await using var context = database.CreateContext();
        var repository = new ThresholdRepository(context);

        var result = await repository.GetByEquipmentAndMeasurementTypeAsync(
            equipment.Id,
            MeasurementType.Temperature,
            cancellationToken);

        var threshold = Assert.Single(result);
        Assert.Equal(equipment.Id, threshold.EquipmentId);
        Assert.Equal(MeasurementType.Temperature, threshold.MeasurementType);
    }

    [Fact]
    public async Task GetByEquipmentAndMeasurementTypeAsync_Should_Return_Warning_And_Critical_Thresholds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var equipment = CreateEquipment();
        await SeedAsync(database, cancellationToken, equipment);
        await SeedThresholdsAsync(
            database,
            cancellationToken,
            new Threshold(equipment.Id, MeasurementType.Temperature, 10.0, 90.0, AlertSeverity.Warning),
            new Threshold(equipment.Id, MeasurementType.Temperature, 20.0, 80.0, AlertSeverity.Critical));

        await using var context = database.CreateContext();
        var repository = new ThresholdRepository(context);

        var result = await repository.GetByEquipmentAndMeasurementTypeAsync(
            equipment.Id,
            MeasurementType.Temperature,
            cancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, threshold => threshold.Severity == AlertSeverity.Warning);
        Assert.Contains(result, threshold => threshold.Severity == AlertSeverity.Critical);
    }

    [Fact]
    public async Task GetByEquipmentAndMeasurementTypeAsync_Should_Exclude_Other_Equipment_And_Types()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var equipment = CreateEquipment();
        var otherEquipment = CreateEquipment();
        await SeedAsync(database, cancellationToken, equipment, otherEquipment);
        await SeedThresholdsAsync(
            database,
            cancellationToken,
            new Threshold(equipment.Id, MeasurementType.Temperature, 10.0, 90.0, AlertSeverity.Warning),
            new Threshold(otherEquipment.Id, MeasurementType.Temperature, 10.0, 90.0, AlertSeverity.Critical),
            new Threshold(equipment.Id, MeasurementType.Load, 10.0, 90.0, AlertSeverity.Critical));

        await using var context = database.CreateContext();
        var repository = new ThresholdRepository(context);

        var result = await repository.GetByEquipmentAndMeasurementTypeAsync(
            equipment.Id,
            MeasurementType.Temperature,
            cancellationToken);

        var threshold = Assert.Single(result);
        Assert.Equal(equipment.Id, threshold.EquipmentId);
        Assert.Equal(MeasurementType.Temperature, threshold.MeasurementType);
        Assert.Equal(AlertSeverity.Warning, threshold.Severity);
    }

    [Fact]
    public async Task GetByEquipmentAndMeasurementTypeAsync_Should_Return_Empty_When_No_Rules_Match()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var equipment = CreateEquipment();
        await SeedAsync(database, cancellationToken, equipment);
        await SeedThresholdsAsync(
            database,
            cancellationToken,
            new Threshold(equipment.Id, MeasurementType.Temperature, 10.0, 90.0, AlertSeverity.Warning));

        await using var context = database.CreateContext();
        var repository = new ThresholdRepository(context);

        var result = await repository.GetByEquipmentAndMeasurementTypeAsync(
            equipment.Id,
            MeasurementType.Voltage,
            cancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByEquipmentAndMeasurementTypeAsync_Should_Support_CancellationToken()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var equipment = CreateEquipment();
        await SeedAsync(database, cancellationToken, equipment);
        await using var context = database.CreateContext();
        var repository = new ThresholdRepository(context);

        var result = await repository.GetByEquipmentAndMeasurementTypeAsync(
            equipment.Id,
            MeasurementType.Temperature,
            cancellationToken);

        Assert.Empty(result);
    }

    private static async Task SeedAsync(
        SqliteTestDatabase database,
        CancellationToken cancellationToken,
        params Equipment[] equipment)
    {
        await using var context = database.CreateContext();
        context.Equipments.AddRange(equipment);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedThresholdsAsync(
        SqliteTestDatabase database,
        CancellationToken cancellationToken,
        params Threshold[] thresholds)
    {
        await using var context = database.CreateContext();
        context.Thresholds.AddRange(thresholds);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static Equipment CreateEquipment() =>
        new("Test Equipment", "Sensor", $"SN-{Guid.NewGuid():N}", "Lab 1");

    private sealed class SqliteTestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<EquipmentDbContext> _options;

        private SqliteTestDatabase(SqliteConnection connection, DbContextOptions<EquipmentDbContext> options)
        {
            _connection = connection;
            _options = options;
        }

        public static async Task<SqliteTestDatabase> CreateAsync(CancellationToken cancellationToken)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(cancellationToken);
            var options = new DbContextOptionsBuilder<EquipmentDbContext>()
                .UseSqlite(connection)
                .Options;
            await using var context = new EquipmentDbContext(options);
            await context.Database.EnsureCreatedAsync(cancellationToken);
            return new SqliteTestDatabase(connection, options);
        }

        public EquipmentDbContext CreateContext() => new(_options);

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
