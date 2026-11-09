using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Services;
using EquipmentMonitoring.Application.Validators;
using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class MeasurementAlertIntegrationTests
{
    private static readonly DateTime ExceedingTimestamp = new(2026, 4, 18, 9, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime NormalTimestamp = new(2026, 4, 18, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task MeasurementService_Should_Persist_Alert_For_Exceeded_Threshold_And_Not_For_Normal_Measurement()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var database = await SqliteTestDatabase.CreateAsync(cancellationToken);
        var equipment = CreateEquipment();
        var threshold = new Threshold(
            equipment.Id,
            MeasurementType.Temperature,
            10.0,
            90.0,
            AlertSeverity.Critical);

        await using (var seedContext = database.CreateContext())
        {
            seedContext.Equipments.Add(equipment);
            seedContext.Thresholds.Add(threshold);
            await seedContext.SaveChangesAsync(cancellationToken);
        }

        MeasurementResponseDto exceededResponse;
        MeasurementResponseDto normalResponse;
        await using (var serviceContext = database.CreateContext())
        {
            var equipmentRepository = new EquipmentRepository(serviceContext);
            var measurementRepository = new MeasurementRepository(serviceContext);
            var thresholdRepository = new ThresholdRepository(serviceContext);
            var alertRepository = new AlertRepository(serviceContext);
            var measurementAlertService = new MeasurementAlertService(
                thresholdRepository,
                alertRepository);
            var validator = new CreateMeasurementDtoValidator(equipmentRepository);
            var measurementService = new MeasurementService(
                validator,
                equipmentRepository,
                measurementRepository,
                measurementAlertService);

            exceededResponse = await measurementService.CreateAsync(
                new CreateMeasurementDto
                {
                    EquipmentId = equipment.Id,
                    Type = MeasurementType.Temperature,
                    Value = 95.0,
                    Timestamp = ExceedingTimestamp
                },
                cancellationToken);

            normalResponse = await measurementService.CreateAsync(
                new CreateMeasurementDto
                {
                    EquipmentId = equipment.Id,
                    Type = MeasurementType.Temperature,
                    Value = 50.0,
                    Timestamp = NormalTimestamp
                },
                cancellationToken);
        }

        await using var verificationContext = database.CreateContext();
        var storedMeasurements = await verificationContext.Measurements
            .AsNoTracking()
            .Where(measurement => measurement.EquipmentId == equipment.Id)
            .OrderBy(measurement => measurement.Timestamp)
            .ToListAsync(cancellationToken);
        var storedAlerts = await verificationContext.Alerts
            .AsNoTracking()
            .Where(alert => alert.EquipmentId == equipment.Id)
            .ToListAsync(cancellationToken);

        Assert.Equal(2, storedMeasurements.Count);
        Assert.Contains(storedMeasurements, measurement => measurement.Id == exceededResponse.Id);
        Assert.Contains(storedMeasurements, measurement => measurement.Id == normalResponse.Id);
        Assert.Equal(DateTimeKind.Utc, exceededResponse.Timestamp.Kind);
        Assert.Equal(DateTimeKind.Utc, normalResponse.Timestamp.Kind);
        Assert.Equal(ExceedingTimestamp, exceededResponse.Timestamp);
        Assert.Equal(NormalTimestamp, normalResponse.Timestamp);

        var alert = Assert.Single(storedAlerts);
        Assert.Equal(equipment.Id, alert.EquipmentId);
        Assert.Equal(exceededResponse.Id, alert.MeasurementId);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
        Assert.Equal(
            "Temperature value 95 is above maximum threshold 90.",
            alert.Message);
        Assert.Equal(ExceedingTimestamp, alert.Timestamp);
        Assert.Equal(DateTimeKind.Utc, alert.Timestamp.Kind);
        Assert.False(alert.IsAcknowledged);
        Assert.DoesNotContain(storedAlerts, item => item.MeasurementId == normalResponse.Id);
    }

    private static Equipment CreateEquipment() =>
        new("Test Equipment", "Sensor", $"SN-{Guid.NewGuid():N}", "Integration lab");

    private sealed class SqliteTestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly DbContextOptions<EquipmentDbContext> options;

        private SqliteTestDatabase(
            SqliteConnection connection,
            DbContextOptions<EquipmentDbContext> options)
        {
            this.connection = connection;
            this.options = options;
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

        public EquipmentDbContext CreateContext() => new(options);

        public async ValueTask DisposeAsync()
        {
            await connection.DisposeAsync();
        }
    }
}
