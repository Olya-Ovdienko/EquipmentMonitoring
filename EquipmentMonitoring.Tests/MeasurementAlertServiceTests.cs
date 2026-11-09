using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.Services;
using EquipmentMonitoring.Domain;
using Moq;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class MeasurementAlertServiceTests
{
    private static readonly DateTime Timestamp = new(2026, 4, 18, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task EvaluateAndPersistAsync_WithNormalValue_DoesNotSaveAlert()
    {
        var measurement = CreateMeasurement(50.0);
        var thresholdRepository = CreateThresholdRepository(
            CreateThreshold(measurement, 10.0, 90.0, AlertSeverity.Warning));
        var alertRepository = CreateAlertRepository();
        var service = CreateService(thresholdRepository.Object, alertRepository.Object);

        await service.EvaluateAndPersistAsync(measurement, TestContext.Current.CancellationToken);

        alertRepository.Verify(
            repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Alert>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateAndPersistAsync_WithValueAboveMaxValue_CreatesAlertWithSeverityAndMessage()
    {
        var measurement = CreateMeasurement(95.0);
        var threshold = CreateThreshold(measurement, 10.0, 90.0, AlertSeverity.Critical);
        var thresholdRepository = CreateThresholdRepository(threshold);
        var alertRepository = CreateAlertRepository();
        IReadOnlyCollection<Alert>? savedAlerts = null;
        alertRepository
            .Setup(repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Alert>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Alert>, CancellationToken>((alerts, _) => savedAlerts = alerts)
            .Returns(Task.CompletedTask);
        var service = CreateService(thresholdRepository.Object, alertRepository.Object);

        await service.EvaluateAndPersistAsync(measurement, TestContext.Current.CancellationToken);

        var alert = Assert.Single(savedAlerts!);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
        Assert.Contains("maximum", alert.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("95", alert.Message);
        Assert.Contains("90", alert.Message);
    }

    [Fact]
    public async Task EvaluateAndPersistAsync_WithValueBelowMinValue_CreatesAlertWithSeverityAndMessage()
    {
        var measurement = CreateMeasurement(5.0);
        var threshold = CreateThreshold(measurement, 10.0, 90.0, AlertSeverity.Warning);
        var thresholdRepository = CreateThresholdRepository(threshold);
        var alertRepository = CreateAlertRepository();
        IReadOnlyCollection<Alert>? savedAlerts = null;
        alertRepository
            .Setup(repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Alert>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Alert>, CancellationToken>((alerts, _) => savedAlerts = alerts)
            .Returns(Task.CompletedTask);
        var service = CreateService(thresholdRepository.Object, alertRepository.Object);

        await service.EvaluateAndPersistAsync(measurement, TestContext.Current.CancellationToken);

        var alert = Assert.Single(savedAlerts!);
        Assert.Equal(AlertSeverity.Warning, alert.Severity);
        Assert.Contains("minimum", alert.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("5", alert.Message);
        Assert.Contains("10", alert.Message);
    }

    [Fact]
    public async Task EvaluateAndPersistAsync_WithoutMatchingThresholds_DoesNotSaveAlert()
    {
        var measurement = CreateMeasurement(95.0);
        var thresholdRepository = CreateThresholdRepository();
        var alertRepository = CreateAlertRepository();
        var service = CreateService(thresholdRepository.Object, alertRepository.Object);

        await service.EvaluateAndPersistAsync(measurement, TestContext.Current.CancellationToken);

        alertRepository.Verify(
            repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Alert>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateAndPersistAsync_WithValueEqualToMinValue_DoesNotSaveAlert()
    {
        var measurement = CreateMeasurement(10.0);
        var thresholdRepository = CreateThresholdRepository(
            CreateThreshold(measurement, 10.0, 90.0, AlertSeverity.Warning));
        var alertRepository = CreateAlertRepository();
        var service = CreateService(thresholdRepository.Object, alertRepository.Object);

        await service.EvaluateAndPersistAsync(measurement, TestContext.Current.CancellationToken);

        alertRepository.Verify(
            repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Alert>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateAndPersistAsync_WithValueEqualToMaxValue_DoesNotSaveAlert()
    {
        var measurement = CreateMeasurement(90.0);
        var thresholdRepository = CreateThresholdRepository(
            CreateThreshold(measurement, 10.0, 90.0, AlertSeverity.Warning));
        var alertRepository = CreateAlertRepository();
        var service = CreateService(thresholdRepository.Object, alertRepository.Object);

        await service.EvaluateAndPersistAsync(measurement, TestContext.Current.CancellationToken);

        alertRepository.Verify(
            repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Alert>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateAndPersistAsync_WithWarningAndCriticalViolations_SavesTwoAlertsInOneBatch()
    {
        var measurement = CreateMeasurement(95.0);
        var warningThreshold = CreateThreshold(measurement, 10.0, 90.0, AlertSeverity.Warning);
        var criticalThreshold = CreateThreshold(measurement, 20.0, 80.0, AlertSeverity.Critical);
        var thresholdRepository = CreateThresholdRepository(warningThreshold, criticalThreshold);
        var alertRepository = CreateAlertRepository();
        IReadOnlyCollection<Alert>? savedAlerts = null;
        alertRepository
            .Setup(repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Alert>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Alert>, CancellationToken>((alerts, _) => savedAlerts = alerts)
            .Returns(Task.CompletedTask);
        var service = CreateService(thresholdRepository.Object, alertRepository.Object);

        await service.EvaluateAndPersistAsync(measurement, TestContext.Current.CancellationToken);

        Assert.Equal(2, savedAlerts!.Count);
        Assert.Contains(savedAlerts, alert => alert.Severity == AlertSeverity.Warning);
        Assert.Contains(savedAlerts, alert => alert.Severity == AlertSeverity.Critical);
        alertRepository.Verify(
            repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Alert>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAndPersistAsync_WithOnlyMinValue_UsesLowerBoundary()
    {
        var measurement = CreateMeasurement(5.0);
        var thresholdRepository = CreateThresholdRepository(
            CreateThreshold(measurement, 10.0, null, AlertSeverity.Warning));
        var alertRepository = CreateAlertRepository();
        var service = CreateService(thresholdRepository.Object, alertRepository.Object);

        await service.EvaluateAndPersistAsync(measurement, TestContext.Current.CancellationToken);

        alertRepository.Verify(
            repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Alert>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAndPersistAsync_WithOnlyMaxValue_UsesUpperBoundary()
    {
        var measurement = CreateMeasurement(95.0);
        var thresholdRepository = CreateThresholdRepository(
            CreateThreshold(measurement, null, 90.0, AlertSeverity.Warning));
        var alertRepository = CreateAlertRepository();
        var service = CreateService(thresholdRepository.Object, alertRepository.Object);

        await service.EvaluateAndPersistAsync(measurement, TestContext.Current.CancellationToken);

        alertRepository.Verify(
            repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Alert>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAndPersistAsync_PreservesMeasurementIdentityAndUtcTimestamp()
    {
        var measurement = CreateMeasurement(95.0);
        var thresholdRepository = CreateThresholdRepository(
            CreateThreshold(measurement, 10.0, 90.0, AlertSeverity.Warning));
        var alertRepository = CreateAlertRepository();
        IReadOnlyCollection<Alert>? savedAlerts = null;
        alertRepository
            .Setup(repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Alert>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Alert>, CancellationToken>((alerts, _) => savedAlerts = alerts)
            .Returns(Task.CompletedTask);
        var service = CreateService(thresholdRepository.Object, alertRepository.Object);

        await service.EvaluateAndPersistAsync(measurement, TestContext.Current.CancellationToken);

        var alert = Assert.Single(savedAlerts!);
        Assert.Equal(measurement.EquipmentId, alert.EquipmentId);
        Assert.Equal(measurement.Id, alert.MeasurementId);
        Assert.Equal(measurement.Timestamp, alert.Timestamp);
        Assert.Equal(DateTimeKind.Utc, alert.Timestamp.Kind);
    }

    [Fact]
    public async Task EvaluateAndPersistAsync_ForwardsCancellationTokenToRepositories()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var measurement = CreateMeasurement(95.0);
        var thresholdRepository = CreateThresholdRepository(
            CreateThreshold(measurement, 10.0, 90.0, AlertSeverity.Warning));
        var alertRepository = CreateAlertRepository();
        alertRepository
            .Setup(repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Alert>>(),
                cancellationToken))
            .Returns(Task.CompletedTask);
        var service = CreateService(thresholdRepository.Object, alertRepository.Object);

        await service.EvaluateAndPersistAsync(measurement, cancellationToken);

        thresholdRepository.Verify(
            repository => repository.GetByEquipmentAndMeasurementTypeAsync(
                measurement.EquipmentId,
                measurement.Type,
                cancellationToken),
            Times.Once);
        alertRepository.Verify(
            repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Alert>>(),
                cancellationToken),
            Times.Once);
    }

    private static MeasurementAlertService CreateService(
        IThresholdRepository thresholdRepository,
        IAlertRepository alertRepository)
    {
        return new MeasurementAlertService(thresholdRepository, alertRepository);
    }

    private static Mock<IThresholdRepository> CreateThresholdRepository(params Threshold[] thresholds)
    {
        var repository = new Mock<IThresholdRepository>();
        repository
            .Setup(item => item.GetByEquipmentAndMeasurementTypeAsync(
                It.IsAny<Guid>(),
                It.IsAny<MeasurementType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(thresholds);
        return repository;
    }

    private static Mock<IAlertRepository> CreateAlertRepository()
    {
        var repository = new Mock<IAlertRepository>();
        repository
            .Setup(item => item.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Alert>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return repository;
    }

    private static Measurement CreateMeasurement(double value)
    {
        return new Measurement(Guid.NewGuid(), MeasurementType.Temperature, value, Timestamp);
    }

    private static Threshold CreateThreshold(
        Measurement measurement,
        double? minValue,
        double? maxValue,
        AlertSeverity severity)
    {
        return new Threshold(measurement.EquipmentId, measurement.Type, minValue, maxValue, severity);
    }
}
