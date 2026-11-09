using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class AlertRepositoryTests
{
    private static readonly DateTime Timestamp = new(2026, 4, 18, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AddRangeAsync_Should_Persist_One_Alert_Through_Sqlite()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var (equipment, measurement) = await SeedEquipmentAndMeasurementAsync(database, cancellationToken);
        var alert = CreateAlert(equipment, measurement, AlertSeverity.Warning);

        await using (var context = database.CreateContext())
        {
            var repository = new AlertRepository(context);

            await repository.AddRangeAsync([alert], cancellationToken);
        }

        await using var verificationContext = database.CreateContext();
        var storedAlert = await verificationContext.Alerts
            .AsNoTracking()
            .SingleAsync(item => item.Id == alert.Id, cancellationToken);

        Assert.Equal(alert.Id, storedAlert.Id);
        Assert.Equal(equipment.Id, storedAlert.EquipmentId);
        Assert.Equal(measurement.Id, storedAlert.MeasurementId);
        Assert.Equal(alert.Message, storedAlert.Message);
        Assert.Equal(alert.Severity, storedAlert.Severity);
        Assert.Equal(alert.Timestamp, storedAlert.Timestamp);
        Assert.Equal(DateTimeKind.Utc, storedAlert.Timestamp.Kind);
        Assert.Equal(alert.IsAcknowledged, storedAlert.IsAcknowledged);
    }

    [Fact]
    public async Task AddRangeAsync_Should_Persist_Multiple_Alerts_Through_Sqlite()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var (equipment, measurement) = await SeedEquipmentAndMeasurementAsync(database, cancellationToken);
        var alerts = new[]
        {
            CreateAlert(equipment, measurement, AlertSeverity.Warning),
            CreateAlert(equipment, measurement, AlertSeverity.Critical)
        };

        await using (var context = database.CreateContext())
        {
            var repository = new AlertRepository(context);

            await repository.AddRangeAsync(alerts, cancellationToken);
        }

        await using var verificationContext = database.CreateContext();
        var storedAlerts = await verificationContext.Alerts
            .AsNoTracking()
            .OrderBy(alert => alert.Id)
            .ToListAsync(cancellationToken);

        Assert.Equal(2, storedAlerts.Count);
        Assert.Contains(storedAlerts, alert => alert.Severity == AlertSeverity.Warning);
        Assert.Contains(storedAlerts, alert => alert.Severity == AlertSeverity.Critical);
        Assert.All(storedAlerts, alert => Assert.Equal(equipment.Id, alert.EquipmentId));
        Assert.All(storedAlerts, alert => Assert.Equal(measurement.Id, alert.MeasurementId));
    }

    [Fact]
    public async Task AddRangeAsync_Should_Preserve_Alert_Fields_And_Measurement_Reference()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var (equipment, measurement) = await SeedEquipmentAndMeasurementAsync(database, cancellationToken);
        var alert = new Alert(
            equipment.Id,
            measurement.Id,
            "Temperature exceeded the allowed threshold.",
            AlertSeverity.Critical,
            Timestamp);

        await using (var context = database.CreateContext())
        {
            var repository = new AlertRepository(context);
            await repository.AddRangeAsync([alert], cancellationToken);
        }

        await using var verificationContext = database.CreateContext();
        var storedAlert = await verificationContext.Alerts
            .AsNoTracking()
            .SingleAsync(item => item.Id == alert.Id, cancellationToken);

        Assert.Equal(alert.Message, storedAlert.Message);
        Assert.Equal(AlertSeverity.Critical, storedAlert.Severity);
        Assert.Equal(measurement.Id, storedAlert.MeasurementId);
        Assert.Equal(Timestamp, storedAlert.Timestamp);
        Assert.False(storedAlert.IsAcknowledged);
    }

    [Fact]
    public async Task AddRangeAsync_Should_Support_CancellationToken()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var (equipment, measurement) = await SeedEquipmentAndMeasurementAsync(database, cancellationToken);
        var alert = CreateAlert(equipment, measurement, AlertSeverity.Warning);

        await using var context = database.CreateContext();
        var repository = new AlertRepository(context);

        await repository.AddRangeAsync([alert], cancellationToken);

        Assert.True(await context.Alerts.AnyAsync(item => item.Id == alert.Id, cancellationToken));
    }

    [Fact]
    public async Task Acknowledged_Alert_Should_Persist_And_Reload_As_Acknowledged()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var (equipment, measurement) = await SeedEquipmentAndMeasurementAsync(database, cancellationToken);
        var alert = CreateAlert(equipment, measurement, AlertSeverity.Warning);

        await using (var context = database.CreateContext())
        {
            var repository = new AlertRepository(context);
            await repository.AddRangeAsync([alert], cancellationToken);
        }

        await using (var context = database.CreateContext())
        {
            var repository = new AlertRepository(context);
            var storedAlert = await repository.GetByIdAsync(alert.Id, cancellationToken);

            Assert.NotNull(storedAlert);
            storedAlert!.Acknowledge();
            await repository.SaveChangesAsync(cancellationToken);
        }

        await using var verificationContext = database.CreateContext();
        var reloadedAlert = await verificationContext.Alerts
            .AsNoTracking()
            .SingleAsync(item => item.Id == alert.Id, cancellationToken);

        Assert.True(reloadedAlert.IsAcknowledged);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Existing_Alert_And_Null_For_Missing_Id()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var (equipment, measurement) = await SeedEquipmentAndMeasurementAsync(database, cancellationToken);
        var alert = CreateAlert(equipment, measurement, AlertSeverity.Warning);

        await using (var context = database.CreateContext())
        {
            var repository = new AlertRepository(context);
            await repository.AddRangeAsync([alert], cancellationToken);
        }

        await using var readContext = database.CreateContext();
        var repositoryForRead = new AlertRepository(readContext);

        var existing = await repositoryForRead.GetByIdAsync(alert.Id, cancellationToken);
        var missing = await repositoryForRead.GetByIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.NotNull(existing);
        Assert.Equal(alert.Id, existing!.Id);
        Assert.Null(missing);
    }

    [Fact]
    public async Task GetAsync_Should_Filter_By_Equipment_Severity_Acknowledgement_And_Inclusive_Utc_Date_Range()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var firstEquipment = new Equipment("Equipment One", "Sensor", $"SN-{Guid.NewGuid():N}", "Lab 1");
        var secondEquipment = new Equipment("Equipment Two", "Sensor", $"SN-{Guid.NewGuid():N}", "Lab 2");
        var firstMeasurement = new Measurement(firstEquipment.Id, MeasurementType.Temperature, 95, Timestamp);
        var secondMeasurement = new Measurement(secondEquipment.Id, MeasurementType.Temperature, 95, Timestamp.AddDays(1));
        var matchingAlert = new Alert(firstEquipment.Id, firstMeasurement.Id, "Matching", AlertSeverity.Critical, Timestamp);
        var wrongSeverity = new Alert(firstEquipment.Id, firstMeasurement.Id, "Wrong severity", AlertSeverity.Warning, Timestamp);
        var wrongEquipment = new Alert(secondEquipment.Id, secondMeasurement.Id, "Wrong equipment", AlertSeverity.Critical, Timestamp);

        await using (var context = database.CreateContext())
        {
            context.AddRange(firstEquipment, secondEquipment, firstMeasurement, secondMeasurement);
            context.AddRange(matchingAlert, wrongSeverity, wrongEquipment);
            await context.SaveChangesAsync(cancellationToken);
        }

        await using (var context = database.CreateContext())
        {
            var repository = new AlertRepository(context);
            var trackedAlert = await repository.GetByIdAsync(matchingAlert.Id, cancellationToken);

            Assert.NotNull(trackedAlert);
            trackedAlert!.Acknowledge();
            await repository.SaveChangesAsync(cancellationToken);

            var result = await repository.GetAsync(
                equipmentId: firstEquipment.Id,
                severity: AlertSeverity.Critical,
                isAcknowledged: true,
                fromDate: Timestamp,
                toDate: Timestamp,
                cancellationToken: cancellationToken);

            var alert = Assert.Single(result);
            Assert.Equal(matchingAlert.Id, alert.Id);
        }
    }

    [Fact]
    public async Task GetAsync_Should_Apply_Combined_Filters_And_Include_Date_Boundaries()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var (equipment, measurement) = await SeedEquipmentAndMeasurementAsync(database, cancellationToken);
        var atStart = new Alert(equipment.Id, measurement.Id, "At start", AlertSeverity.Warning, Timestamp);
        var atEnd = new Alert(equipment.Id, measurement.Id, "At end", AlertSeverity.Warning, Timestamp.AddHours(1));
        var outsideRange = new Alert(equipment.Id, measurement.Id, "Outside", AlertSeverity.Warning, Timestamp.AddHours(2));
        atStart.Acknowledge();
        atEnd.Acknowledge();

        await using (var context = database.CreateContext())
        {
            context.AddRange(atStart, atEnd, outsideRange);
            await context.SaveChangesAsync(cancellationToken);
        }

        await using var readContext = database.CreateContext();
        var repository = new AlertRepository(readContext);
        var result = await repository.GetAsync(
            equipmentId: equipment.Id,
            severity: AlertSeverity.Warning,
            isAcknowledged: true,
            fromDate: Timestamp,
            toDate: Timestamp.AddHours(1),
            cancellationToken: cancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, alert => alert.Id == atStart.Id);
        Assert.Contains(result, alert => alert.Id == atEnd.Id);
        Assert.DoesNotContain(result, alert => alert.Id == outsideRange.Id);
    }

    private static async Task<(Equipment Equipment, Measurement Measurement)> SeedEquipmentAndMeasurementAsync(
        SqliteTestDatabase database,
        CancellationToken cancellationToken)
    {
        var equipment = new Equipment("Test Equipment", "Sensor", $"SN-{Guid.NewGuid():N}", "Lab 1");
        var measurement = new Measurement(equipment.Id, MeasurementType.Temperature, 95.0, Timestamp);

        await using var context = database.CreateContext();
        context.Equipments.Add(equipment);
        context.Measurements.Add(measurement);
        await context.SaveChangesAsync(cancellationToken);

        return (equipment, measurement);
    }

    private static Alert CreateAlert(
        Equipment equipment,
        Measurement measurement,
        AlertSeverity severity) => new(
            equipment.Id,
            measurement.Id,
            "Temperature exceeded the allowed threshold.",
            severity,
            Timestamp);

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
