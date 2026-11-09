using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class AlertApiTests
{
    private const string AlertsEndpoint = "/api/v1/alerts";
    private static readonly DateTime FilterStart = new(2026, 4, 18, 9, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime FilterEnd = FilterStart.AddHours(1);

    [Fact]
    public async Task Get_Alerts_When_None_Exist_Returns_Empty_Array()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.GetAsync(AlertsEndpoint, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var alerts = await response.Content.ReadFromJsonAsync<AlertResponseDto[]>(cancellationToken);
        Assert.NotNull(alerts);
        Assert.Empty(alerts);
    }

    [Fact]
    public async Task Get_Alerts_Returns_Alert_Response_Objects()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);
        var seeded = await api.SeedAlertAsync(AlertSeverity.Critical, FilterStart, false, cancellationToken);

        using var response = await api.Client.GetAsync(AlertsEndpoint, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var alerts = await response.Content.ReadFromJsonAsync<AlertResponseDto[]>(cancellationToken);
        var alert = Assert.Single(alerts!);
        Assert.Equal(seeded.AlertId, alert.Id);
        Assert.Equal(seeded.EquipmentId, alert.EquipmentId);
        Assert.Equal(seeded.MeasurementId, alert.MeasurementId);
        Assert.Equal("Temperature exceeded the allowed threshold.", alert.Message);
        Assert.Equal(nameof(AlertSeverity.Critical), alert.Severity);
        Assert.Equal(FilterStart, alert.Timestamp);
        Assert.False(alert.IsAcknowledged);
    }

    [Fact]
    public async Task Get_Alerts_Filters_By_Severity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);
        var warning = await api.SeedAlertAsync(AlertSeverity.Warning, FilterStart, false, cancellationToken);
        await api.SeedAlertAsync(AlertSeverity.Critical, FilterStart, false, cancellationToken);

        using var response = await api.Client.GetAsync(
            $"{AlertsEndpoint}?severity={AlertSeverity.Warning}", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var alerts = await response.Content.ReadFromJsonAsync<AlertResponseDto[]>(cancellationToken);
        var alert = Assert.Single(alerts!);
        Assert.Equal(warning.AlertId, alert.Id);
    }

    [Fact]
    public async Task Get_Alerts_Filters_By_Acknowledgement()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);
        var acknowledged = await api.SeedAlertAsync(AlertSeverity.Warning, FilterStart, true, cancellationToken);
        await api.SeedAlertAsync(AlertSeverity.Warning, FilterStart, false, cancellationToken);

        using var response = await api.Client.GetAsync(
            $"{AlertsEndpoint}?isAcknowledged=true", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var alerts = await response.Content.ReadFromJsonAsync<AlertResponseDto[]>(cancellationToken);
        var alert = Assert.Single(alerts!);
        Assert.Equal(acknowledged.AlertId, alert.Id);
        Assert.True(alert.IsAcknowledged);
    }

    [Fact]
    public async Task Get_Alerts_Filters_By_Inclusive_Utc_Date_Range()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);
        var atStart = await api.SeedAlertAsync(AlertSeverity.Warning, FilterStart, false, cancellationToken);
        var atEnd = await api.SeedAlertAsync(AlertSeverity.Warning, FilterEnd, false, cancellationToken);
        await api.SeedAlertAsync(AlertSeverity.Warning, FilterEnd.AddTicks(1), false, cancellationToken);

        using var response = await api.Client.GetAsync(
            BuildAlertsUrl(fromDate: FilterStart, toDate: FilterEnd), cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var alerts = await response.Content.ReadFromJsonAsync<AlertResponseDto[]>(cancellationToken);
        Assert.Equal(2, alerts!.Length);
        Assert.Contains(alerts, alert => alert.Id == atStart.AlertId);
        Assert.Contains(alerts, alert => alert.Id == atEnd.AlertId);
    }

    [Fact]
    public async Task Get_Alerts_Supports_Combined_Filters()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);
        var matching = await api.SeedAlertAsync(AlertSeverity.Critical, FilterEnd, true, cancellationToken);
        await api.SeedAlertAsync(AlertSeverity.Critical, FilterEnd.AddTicks(1), true, cancellationToken);
        await api.SeedAlertAsync(AlertSeverity.Warning, FilterEnd, true, cancellationToken);
        await api.SeedAlertAsync(AlertSeverity.Critical, FilterEnd, false, cancellationToken);

        using var response = await api.Client.GetAsync(
            BuildAlertsUrl(AlertSeverity.Critical, true, FilterStart, FilterEnd), cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var alerts = await response.Content.ReadFromJsonAsync<AlertResponseDto[]>(cancellationToken);
        var alert = Assert.Single(alerts!);
        Assert.Equal(matching.AlertId, alert.Id);
    }

    [Theory]
    [InlineData("Unsupported")]
    [InlineData("999")]
    public async Task Get_Alerts_With_Invalid_Severity_Returns_BadRequest(string severity)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.GetAsync($"{AlertsEndpoint}?severity={severity}", cancellationToken);

        await AssertBadRequestAsync(response, cancellationToken);
    }

    [Theory]
    [InlineData("2026-04-18T09:30:00")]
    [InlineData("2026-04-18T09:30:00+02:00")]
    [InlineData("not-a-date")]
    public async Task Get_Alerts_With_Invalid_Date_Returns_BadRequest(string date)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.GetAsync($"{AlertsEndpoint}?fromDate={Uri.EscapeDataString(date)}", cancellationToken);

        await AssertBadRequestAsync(response, cancellationToken);
    }

    [Fact]
    public async Task Get_Alerts_When_FromDate_Is_After_ToDate_Returns_BadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.GetAsync(
            BuildAlertsUrl(fromDate: FilterEnd, toDate: FilterStart), cancellationToken);

        await AssertBadRequestAsync(response, cancellationToken);
    }

    [Fact]
    public async Task Get_Equipment_Alerts_Returns_Matching_Equipment_Alerts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);
        var matching = await api.SeedAlertAsync(AlertSeverity.Warning, FilterStart, false, cancellationToken);
        await api.SeedAlertAsync(AlertSeverity.Warning, FilterStart, false, cancellationToken, createDifferentEquipment: true);

        using var response = await api.Client.GetAsync(
            $"/api/v1/equipment/{matching.EquipmentId}/alerts", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var alerts = await response.Content.ReadFromJsonAsync<AlertResponseDto[]>(cancellationToken);
        var alert = Assert.Single(alerts!);
        Assert.Equal(matching.AlertId, alert.Id);
        Assert.Equal(matching.EquipmentId, alert.EquipmentId);
    }

    [Fact]
    public async Task Get_Equipment_Alerts_For_Existing_Equipment_Without_Alerts_Returns_Empty_Array()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);
        var equipmentId = await api.CreateEquipmentAsync(cancellationToken);

        using var response = await api.Client.GetAsync(
            $"/api/v1/equipment/{equipmentId}/alerts", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var alerts = await response.Content.ReadFromJsonAsync<AlertResponseDto[]>(cancellationToken);
        Assert.Empty(alerts!);
    }

    [Fact]
    public async Task Get_Equipment_Alerts_For_Missing_Equipment_Returns_NotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.GetAsync(
            $"/api/v1/equipment/{Guid.NewGuid()}/alerts", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Equipment_Alerts_With_Invalid_Filter_Returns_BadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);
        var equipmentId = await api.CreateEquipmentAsync(cancellationToken);

        using var response = await api.Client.GetAsync(
            $"/api/v1/equipment/{equipmentId}/alerts?fromDate={Uri.EscapeDataString("2026-04-18T09:30:00")}",
            cancellationToken);

        await AssertBadRequestAsync(response, cancellationToken);
    }

    [Fact]
    public async Task Patch_Acknowledge_Returns_Updated_Response_And_Persists_Change()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);
        var seeded = await api.SeedAlertAsync(AlertSeverity.Warning, FilterStart, false, cancellationToken);

        using var response = await api.Client.PatchAsync(
            $"{AlertsEndpoint}/{seeded.AlertId}/acknowledge", content: null, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AlertResponseDto>(cancellationToken);
        Assert.NotNull(result);
        Assert.Equal(seeded.AlertId, result.Id);
        Assert.True(result.IsAcknowledged);

        await using var scope = api.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentDbContext>();
        var storedAlert = await dbContext.Alerts.AsNoTracking()
            .SingleAsync(alert => alert.Id == seeded.AlertId, cancellationToken);
        Assert.True(storedAlert.IsAcknowledged);
    }

    [Fact]
    public async Task Patch_Acknowledge_Repeatedly_Succeeds_Without_Duplicating_Alert()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);
        var seeded = await api.SeedAlertAsync(AlertSeverity.Warning, FilterStart, false, cancellationToken);
        var endpoint = $"{AlertsEndpoint}/{seeded.AlertId}/acknowledge";

        using var firstResponse = await api.Client.PatchAsync(endpoint, content: null, cancellationToken);
        using var secondResponse = await api.Client.PatchAsync(endpoint, content: null, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        await using var scope = api.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentDbContext>();
        Assert.Equal(1, await dbContext.Alerts.CountAsync(alert => alert.Id == seeded.AlertId, cancellationToken));
        Assert.Equal(1, await dbContext.Alerts.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task Patch_Acknowledge_For_Missing_Alert_Returns_NotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.PatchAsync(
            $"{AlertsEndpoint}/{Guid.NewGuid()}/acknowledge", content: null, cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Patch_Acknowledge_With_Invalid_Route_Id_Returns_BadRequest(string id)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await AlertApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.PatchAsync(
            $"{AlertsEndpoint}/{id}/acknowledge", content: null, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string BuildAlertsUrl(
        AlertSeverity? severity = null,
        bool? isAcknowledged = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = new List<string>();
        if (severity.HasValue)
        {
            query.Add($"severity={severity.Value}");
        }

        if (isAcknowledged.HasValue)
        {
            query.Add($"isAcknowledged={isAcknowledged.Value.ToString().ToLowerInvariant()}");
        }

        if (fromDate.HasValue)
        {
            query.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.ToString("O", CultureInfo.InvariantCulture))}");
        }

        if (toDate.HasValue)
        {
            query.Add($"toDate={Uri.EscapeDataString(toDate.Value.ToString("O", CultureInfo.InvariantCulture))}");
        }

        return query.Count == 0 ? AlertsEndpoint : $"{AlertsEndpoint}?{string.Join("&", query)}";
    }

    private static async Task AssertBadRequestAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
    }

    private sealed class AlertApiTestHost : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AlertApiFactory _factory;

        private AlertApiTestHost(SqliteConnection connection, AlertApiFactory factory, HttpClient client)
        {
            _connection = connection;
            _factory = factory;
            Client = client;
        }

        public HttpClient Client { get; }
        public IServiceProvider Services => _factory.Services;

        public static async Task<AlertApiTestHost> CreateAsync(CancellationToken cancellationToken)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(cancellationToken);
            var factory = new AlertApiFactory(connection);
            HttpClient? client = null;

            try
            {
                client = factory.CreateClient();
                await using var scope = factory.Services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentDbContext>();
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                return new AlertApiTestHost(connection, factory, client);
            }
            catch
            {
                client?.Dispose();
                await factory.DisposeAsync();
                await connection.DisposeAsync();
                throw;
            }
        }

        public async Task<Guid> CreateEquipmentAsync(CancellationToken cancellationToken)
        {
            var equipment = new Equipment(
                "Alert Test Equipment",
                "Sensor",
                $"SN-{Guid.NewGuid():N}",
                "Test lab");

            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentDbContext>();
            dbContext.Equipments.Add(equipment);
            await dbContext.SaveChangesAsync(cancellationToken);
            return equipment.Id;
        }

        public async Task<SeededAlert> SeedAlertAsync(
            AlertSeverity severity,
            DateTime timestamp,
            bool acknowledged,
        CancellationToken cancellationToken,
        bool createDifferentEquipment = false)
        {
            var equipment = new Equipment(
                "Alert Test Equipment",
                "Sensor",
                $"SN-{Guid.NewGuid():N}",
                "Test lab");
            var measurement = new Measurement(equipment.Id, MeasurementType.Temperature, 95, timestamp);
            var alert = new Alert(
                equipment.Id,
                measurement.Id,
                "Temperature exceeded the allowed threshold.",
                severity,
                timestamp);

            if (acknowledged)
            {
                alert.Acknowledge();
            }

            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentDbContext>();
            dbContext.AddRange(equipment, measurement, alert);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new SeededAlert(equipment.Id, measurement.Id, alert.Id);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _factory.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed record SeededAlert(Guid EquipmentId, Guid MeasurementId, Guid AlertId);

    private sealed class AlertApiFactory(SqliteConnection connection) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<EquipmentDbContext>>();
                services.RemoveAll<EquipmentDbContext>();
                services.AddDbContext<EquipmentDbContext>(options => options.UseSqlite(connection));
            });
        }
    }
}
