using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Services;
using EquipmentMonitoring.Domain;
using FluentValidation;
using Moq;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class AlertServiceTests
{
    private static readonly DateTime FromDate = new(2026, 4, 18, 9, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime ToDate = FromDate.AddHours(1);

    [Fact]
    public async Task GetAsync_Should_Map_Alert_Entities_To_Response_Dtos()
    {
        var alert = CreateAlert();
        var alertRepository = CreateAlertRepository(alert);
        var service = CreateService(alertRepository.Object);

        var result = await service.GetAsync(
            severity: null,
            isAcknowledged: null,
            fromDate: null,
            toDate: null,
            TestContext.Current.CancellationToken);

        var response = Assert.Single(result);
        Assert.Equal(alert.Id, response.Id);
        Assert.Equal(alert.EquipmentId, response.EquipmentId);
        Assert.Equal(alert.MeasurementId, response.MeasurementId);
        Assert.Equal(alert.Message, response.Message);
        Assert.Equal(nameof(AlertSeverity.Warning), response.Severity);
        Assert.Equal(alert.Timestamp, response.Timestamp);
        Assert.False(response.IsAcknowledged);
    }

    [Fact]
    public async Task GetAsync_Should_Return_Empty_When_No_Alerts_Match()
    {
        var alertRepository = CreateAlertRepository();
        var service = CreateService(alertRepository.Object);

        var result = await service.GetAsync(
            AlertSeverity.Critical,
            isAcknowledged: null,
            fromDate: null,
            toDate: null,
            TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData((AlertSeverity)999)]
    public async Task GetAsync_Should_Reject_Invalid_Severity(AlertSeverity severity)
    {
        var service = CreateService(CreateAlertRepository().Object);

        await Assert.ThrowsAsync<ValidationException>(() => service.GetAsync(
            severity,
            isAcknowledged: null,
            fromDate: null,
            toDate: null,
            TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task GetAsync_Should_Reject_NonUtc_Date_Filters(DateTimeKind kind)
    {
        var service = CreateService(CreateAlertRepository().Object);
        var nonUtcDate = DateTime.SpecifyKind(FromDate, kind);

        await Assert.ThrowsAsync<ValidationException>(() => service.GetAsync(
            severity: null,
            isAcknowledged: null,
            fromDate: nonUtcDate,
            toDate: null,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAsync_Should_Reject_Date_Range_When_FromDate_Is_After_ToDate()
    {
        var service = CreateService(CreateAlertRepository().Object);

        await Assert.ThrowsAsync<ValidationException>(() => service.GetAsync(
            severity: null,
            isAcknowledged: null,
            fromDate: ToDate,
            toDate: FromDate,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByEquipmentIdAsync_Should_Return_NotFound_For_Missing_Equipment()
    {
        var equipmentRepository = new Mock<IEquipmentRepository>();
        equipmentRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Equipment?)null);
        var service = CreateService(CreateAlertRepository().Object, equipmentRepository.Object);

        await Assert.ThrowsAsync<ValidationException>(() => service.GetByEquipmentIdAsync(
            Guid.NewGuid(),
            severity: null,
            isAcknowledged: null,
            fromDate: null,
            toDate: null,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AcknowledgeAsync_Should_Acknowledge_Existing_Alert_And_Persist_Change()
    {
        var alert = CreateAlert();
        var alertRepository = CreateAlertRepository(alert);
        var service = CreateService(alertRepository.Object);

        var result = await service.AcknowledgeAsync(alert.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(alert.IsAcknowledged);
        Assert.True(result!.IsAcknowledged);
        alertRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AcknowledgeAsync_Should_Handle_Repeated_Acknowledgement_Idempotently()
    {
        var alert = CreateAlert();
        var alertRepository = CreateAlertRepository(alert);
        var service = CreateService(alertRepository.Object);

        var firstResult = await service.AcknowledgeAsync(alert.Id, TestContext.Current.CancellationToken);
        var secondResult = await service.AcknowledgeAsync(alert.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);
        Assert.True(secondResult!.IsAcknowledged);
        alertRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task AcknowledgeAsync_Should_Return_Null_For_Missing_Alert()
    {
        var alertRepository = CreateAlertRepository();
        var service = CreateService(alertRepository.Object);

        var result = await service.AcknowledgeAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Null(result);
        alertRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Alert CreateAlert()
    {
        return new Alert(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Temperature exceeded the allowed threshold.",
            AlertSeverity.Warning,
            FromDate);
    }

    private static AlertService CreateService(
        IAlertRepository alertRepository,
        IEquipmentRepository? equipmentRepository = null)
    {
        return new AlertService(
            alertRepository,
            equipmentRepository ?? CreateExistingEquipmentRepository().Object);
    }

    private static Mock<IAlertRepository> CreateAlertRepository(params Alert[] alerts)
    {
        var repository = new Mock<IAlertRepository>();
        repository
            .Setup(item => item.GetAsync(
                It.IsAny<Guid?>(),
                It.IsAny<AlertSeverity?>(),
                It.IsAny<bool?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);
        repository
            .Setup(item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => alerts.SingleOrDefault(alert => alert.Id == id));
        repository
            .Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return repository;
    }

    private static Mock<IEquipmentRepository> CreateExistingEquipmentRepository()
    {
        var repository = new Mock<IEquipmentRepository>();
        repository
            .Setup(item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Equipment("Test Equipment", "Sensor", $"SN-{Guid.NewGuid():N}", "Lab"));
        return repository;
    }
}
