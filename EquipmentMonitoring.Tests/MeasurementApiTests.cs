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

public class MeasurementApiTests
{
    private const string MeasurementsEndpoint = "/api/v1/measurements";
    private static readonly DateTime FilterStart = new(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FilterMiddle = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FilterEnd = new(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Post_Valid_Measurement_Returns_Created()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var equipment = await api.CreateEquipmentAsync(cancellationToken);

        using var response = await api.Client.PostAsJsonAsync(
            MeasurementsEndpoint,
            CreateValidRequest(equipment.Id),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Post_Valid_Measurement_Returns_All_Response_Fields()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var equipment = await api.CreateEquipmentAsync(cancellationToken);
        var request = CreateValidRequest(equipment.Id);

        using var response = await api.Client.PostAsJsonAsync(MeasurementsEndpoint, request, cancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MeasurementResponseDto>(cancellationToken);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(equipment.Id, result.EquipmentId);
        Assert.Equal(nameof(MeasurementType.Temperature), result.Type);
        Assert.Equal(request.Value, result.Value);
        Assert.Equal("°C", result.Unit);
        Assert.Equal(DateTimeKind.Utc, result.Timestamp.Kind);
        Assert.NotEqual(default, result.Timestamp);
    }

    [Fact]
    public async Task Post_Valid_Measurement_Persists_It_In_Sqlite()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var equipment = await api.CreateEquipmentAsync(cancellationToken);
        var request = CreateValidRequest(equipment.Id);

        using var response = await api.Client.PostAsJsonAsync(MeasurementsEndpoint, request, cancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MeasurementResponseDto>(cancellationToken);
        Assert.NotNull(result);

        await using var scope = api.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentDbContext>();
        var storedMeasurement = await dbContext.Measurements
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == result.Id, cancellationToken);

        Assert.NotNull(storedMeasurement);
        Assert.Equal(equipment.Id, storedMeasurement.EquipmentId);
        Assert.Equal(request.Value, storedMeasurement.Value);
    }

    [Fact]
    public async Task Post_Without_Timestamp_Uses_Current_Utc_Time()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var equipment = await api.CreateEquipmentAsync(cancellationToken);
        var beforeRequest = DateTime.UtcNow;

        using var response = await api.Client.PostAsJsonAsync(
            MeasurementsEndpoint,
            CreateValidRequest(equipment.Id),
            cancellationToken);

        var afterResponse = DateTime.UtcNow;
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MeasurementResponseDto>(cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result.Timestamp.Kind);
        Assert.InRange(result.Timestamp, beforeRequest, afterResponse);
    }

    [Fact]
    public async Task Post_With_Utc_Timestamp_Preserves_It()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var equipment = await api.CreateEquipmentAsync(cancellationToken);
        var timestamp = DateTime.UtcNow.AddMinutes(-10);

        using var response = await api.Client.PostAsJsonAsync(
            MeasurementsEndpoint,
            CreateValidRequest(equipment.Id, timestamp),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MeasurementResponseDto>(cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(timestamp, result.Timestamp);
        Assert.Equal(DateTimeKind.Utc, result.Timestamp.Kind);
    }

    [Fact]
    public async Task Post_Without_EquipmentId_Returns_BadRequest_With_Field_Error()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var request = new { Type = MeasurementType.Temperature, Value = 20.5 };

        using var response = await api.Client.PostAsJsonAsync(MeasurementsEndpoint, request, cancellationToken);

        await AssertBadRequestHasFieldsAsync(response, cancellationToken, nameof(CreateMeasurementDto.EquipmentId));
    }

    [Fact]
    public async Task Post_With_Empty_EquipmentId_Returns_BadRequest_With_Field_Error()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var request = CreateValidRequest(Guid.Empty);

        using var response = await api.Client.PostAsJsonAsync(MeasurementsEndpoint, request, cancellationToken);

        await AssertBadRequestHasFieldsAsync(response, cancellationToken, nameof(CreateMeasurementDto.EquipmentId));
    }

    [Fact]
    public async Task Post_With_Nonexistent_EquipmentId_Returns_NotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var request = CreateValidRequest(Guid.NewGuid());

        using var response = await api.Client.PostAsJsonAsync(MeasurementsEndpoint, request, cancellationToken);

        await AssertNotFoundProblemDetailsAsync(response, cancellationToken);
    }

    [Fact]
    public async Task Post_With_Unsupported_Type_Returns_BadRequest_With_Field_Error()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var request = new { EquipmentId = (await api.CreateEquipmentAsync(cancellationToken)).Id, Type = 999, Value = 20.5 };

        using var response = await api.Client.PostAsJsonAsync(MeasurementsEndpoint, request, cancellationToken);

        await AssertBadRequestHasFieldsAsync(response, cancellationToken, nameof(CreateMeasurementDto.Type));
    }

    [Theory]
    [InlineData(MeasurementType.Voltage)]
    [InlineData(MeasurementType.Load)]
    public async Task Post_With_Negative_Voltage_Or_Load_Returns_BadRequest(MeasurementType type)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var equipment = await api.CreateEquipmentAsync(cancellationToken);
        var request = new { EquipmentId = equipment.Id, Type = type, Value = -0.5 };

        using var response = await api.Client.PostAsJsonAsync(MeasurementsEndpoint, request, cancellationToken);

        await AssertBadRequestHasFieldsAsync(response, cancellationToken, nameof(CreateMeasurementDto.Value));
    }

    [Fact]
    public async Task Post_With_Future_Timestamp_Returns_BadRequest_With_Field_Error()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var equipment = await api.CreateEquipmentAsync(cancellationToken);
        var request = CreateValidRequest(equipment.Id, DateTime.UtcNow.AddHours(1));

        using var response = await api.Client.PostAsJsonAsync(MeasurementsEndpoint, request, cancellationToken);

        await AssertBadRequestHasFieldsAsync(response, cancellationToken, nameof(CreateMeasurementDto.Timestamp));
    }

    [Fact]
    public async Task Post_With_Multiple_Invalid_Fields_Returns_Field_Names_And_Messages()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var request = new
        {
            EquipmentId = Guid.Empty,
            Type = 999,
            Value = -1.0,
            Timestamp = DateTime.UtcNow.AddHours(1)
        };

        using var response = await api.Client.PostAsJsonAsync(MeasurementsEndpoint, request, cancellationToken);

        await AssertBadRequestHasFieldsAsync(
            response,
            cancellationToken,
            nameof(CreateMeasurementDto.EquipmentId),
            nameof(CreateMeasurementDto.Type),
            nameof(CreateMeasurementDto.Value),
            nameof(CreateMeasurementDto.Timestamp));
    }

    [Fact]
    public async Task Get_For_Existing_Equipment_Returns_Ok()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var equipment = await api.CreateEquipmentAsync(cancellationToken);

        using var response = await api.Client.GetAsync(MeasurementsFor(equipment.Id), cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_Returns_Only_Measurements_For_Requested_Equipment()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var firstEquipment = await api.CreateEquipmentAsync(cancellationToken);
        var secondEquipment = await api.CreateEquipmentAsync(cancellationToken);
        await api.AddMeasurementAsync(firstEquipment.Id, MeasurementType.Temperature, 18.5, FilterStart, cancellationToken);
        await api.AddMeasurementAsync(secondEquipment.Id, MeasurementType.Voltage, 220, FilterStart, cancellationToken);

        using var response = await api.Client.GetAsync(MeasurementsFor(firstEquipment.Id), cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<MeasurementResponseDto>>(cancellationToken);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.All(result, item => Assert.Equal(firstEquipment.Id, item.EquipmentId));
    }

    [Fact]
    public async Task Get_Returns_All_Measurement_Response_Fields()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var equipment = await api.CreateEquipmentAsync(cancellationToken);
        var measurement = await api.AddMeasurementAsync(
            equipment.Id,
            MeasurementType.Voltage,
            230.75,
            FilterMiddle,
            cancellationToken);

        using var response = await api.Client.GetAsync(MeasurementsFor(equipment.Id), cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<MeasurementResponseDto>>(cancellationToken);

        Assert.NotNull(result);
        var item = Assert.Single(result);
        Assert.Equal(measurement.Id, item.Id);
        Assert.Equal(equipment.Id, item.EquipmentId);
        Assert.Equal(nameof(MeasurementType.Voltage), item.Type);
        Assert.Equal(230.75, item.Value);
        Assert.Equal("V", item.Unit);
        Assert.Equal(FilterMiddle, item.Timestamp);
        Assert.Equal(DateTimeKind.Utc, item.Timestamp.Kind);
    }

    [Fact]
    public async Task Get_For_Equipment_With_No_Measurements_Returns_Empty_List_And_Ok()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var equipment = await api.CreateEquipmentAsync(cancellationToken);

        using var response = await api.Client.GetAsync(MeasurementsFor(equipment.Id), cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<MeasurementResponseDto>>(cancellationToken);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Get_For_Nonexistent_Equipment_Returns_NotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.GetAsync(MeasurementsFor(Guid.NewGuid()), cancellationToken);

        await AssertNotFoundProblemDetailsAsync(response, cancellationToken);
    }

    [Fact]
    public async Task Get_With_Type_Filter_Returns_Only_Matching_Type()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var equipment = await api.CreateEquipmentAsync(cancellationToken);
        await api.AddMeasurementAsync(equipment.Id, MeasurementType.Temperature, 20, FilterMiddle, cancellationToken);
        await api.AddMeasurementAsync(equipment.Id, MeasurementType.Voltage, 220, FilterMiddle, cancellationToken);

        using var response = await api.Client.GetAsync(MeasurementsFor(equipment.Id, type: "Voltage"), cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<MeasurementResponseDto>>(cancellationToken);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(nameof(MeasurementType.Voltage), result[0].Type);
    }

    [Fact]
    public async Task Get_With_StartDate_Filter_Excludes_Older_Measurements()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var equipment = await api.CreateEquipmentAsync(cancellationToken);
        await api.AddMeasurementAsync(equipment.Id, MeasurementType.Temperature, 10, FilterStart.AddTicks(-1), cancellationToken);
        await api.AddMeasurementAsync(equipment.Id, MeasurementType.Temperature, 20, FilterStart, cancellationToken);

        using var response = await api.Client.GetAsync(
            MeasurementsFor(equipment.Id, startDate: FilterStart),
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<MeasurementResponseDto>>(cancellationToken);

        Assert.NotNull(result);
        var item = Assert.Single(result);
        Assert.Equal(20, item.Value);
    }

    [Fact]
    public async Task Get_With_EndDate_Filter_Excludes_Later_Measurements()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var equipment = await api.CreateEquipmentAsync(cancellationToken);
        await api.AddMeasurementAsync(equipment.Id, MeasurementType.Temperature, 20, FilterEnd, cancellationToken);
        await api.AddMeasurementAsync(equipment.Id, MeasurementType.Temperature, 30, FilterEnd.AddTicks(1), cancellationToken);

        using var response = await api.Client.GetAsync(
            MeasurementsFor(equipment.Id, endDate: FilterEnd),
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<MeasurementResponseDto>>(cancellationToken);

        Assert.NotNull(result);
        var item = Assert.Single(result);
        Assert.Equal(20, item.Value);
    }

    [Fact]
    public async Task Get_With_Type_And_Date_Range_Filters_Returns_Matching_Measurements()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var equipment = await api.CreateEquipmentAsync(cancellationToken);
        await api.AddMeasurementAsync(equipment.Id, MeasurementType.Voltage, 220, FilterStart.AddDays(-1), cancellationToken);
        await api.AddMeasurementAsync(equipment.Id, MeasurementType.Temperature, 20, FilterMiddle, cancellationToken);
        await api.AddMeasurementAsync(equipment.Id, MeasurementType.Voltage, 230, FilterMiddle, cancellationToken);
        await api.AddMeasurementAsync(equipment.Id, MeasurementType.Voltage, 240, FilterEnd.AddDays(1), cancellationToken);

        using var response = await api.Client.GetAsync(
            MeasurementsFor(equipment.Id, "Voltage", FilterStart, FilterEnd),
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<MeasurementResponseDto>>(cancellationToken);

        Assert.NotNull(result);
        var item = Assert.Single(result);
        Assert.Equal(230, item.Value);
        Assert.Equal(nameof(MeasurementType.Voltage), item.Type);
        Assert.Equal(FilterMiddle, item.Timestamp);
    }

    [Fact]
    public async Task Get_After_Post_Returns_New_Measurement()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await MeasurementApiTestHost.CreateAsync(cancellationToken);
        var equipment = await api.CreateEquipmentAsync(cancellationToken);
        var request = CreateValidRequest(equipment.Id, FilterMiddle);

        using var postResponse = await api.Client.PostAsJsonAsync(MeasurementsEndpoint, request, cancellationToken);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        using var getResponse = await api.Client.GetAsync(MeasurementsFor(equipment.Id), cancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var result = await getResponse.Content.ReadFromJsonAsync<List<MeasurementResponseDto>>(cancellationToken);

        Assert.NotNull(result);
        var measurement = Assert.Single(result);
        Assert.Equal(equipment.Id, measurement.EquipmentId);
        Assert.Equal(request.Value, measurement.Value);
    }

    private static CreateMeasurementDto CreateValidRequest(Guid equipmentId, DateTime? timestamp = null) => new()
    {
        EquipmentId = equipmentId,
        Type = MeasurementType.Temperature,
        Value = 21.75,
        Timestamp = timestamp
    };

    private static string MeasurementsFor(
        Guid equipmentId,
        string? type = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = new List<string>();
        if (type is not null)
        {
            query.Add($"type={Uri.EscapeDataString(type)}");
        }

        if (startDate.HasValue)
        {
            query.Add($"startDate={Uri.EscapeDataString(startDate.Value.ToString("O", CultureInfo.InvariantCulture))}");
        }

        if (endDate.HasValue)
        {
            query.Add($"endDate={Uri.EscapeDataString(endDate.Value.ToString("O", CultureInfo.InvariantCulture))}");
        }

        var queryString = query.Count == 0 ? string.Empty : $"?{string.Join("&", query)}";
        return $"/api/v1/equipment/{equipmentId}/measurements{queryString}";
    }

    private static async Task AssertBadRequestHasFieldsAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken,
        params string[] fieldNames)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var errors = document.RootElement.GetProperty("errors");

        foreach (var fieldName in fieldNames)
        {
            Assert.True(errors.TryGetProperty(fieldName, out var messages));
            Assert.Equal(JsonValueKind.Array, messages.ValueKind);
            Assert.True(messages.GetArrayLength() > 0);
            Assert.All(messages.EnumerateArray(), message => Assert.False(string.IsNullOrWhiteSpace(message.GetString())));
        }
    }

    private static async Task AssertNotFoundProblemDetailsAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var problemDetails = document.RootElement;

        Assert.Equal(404, problemDetails.GetProperty("status").GetInt32());
        Assert.Equal("Equipment not found.", problemDetails.GetProperty("title").GetString());
    }

    private sealed class MeasurementApiTestHost : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly MeasurementApiFactory _factory;

        private MeasurementApiTestHost(SqliteConnection connection, MeasurementApiFactory factory, HttpClient client)
        {
            _connection = connection;
            _factory = factory;
            Client = client;
        }

        public HttpClient Client { get; }
        public IServiceProvider Services => _factory.Services;

        public static async Task<MeasurementApiTestHost> CreateAsync(CancellationToken cancellationToken)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(cancellationToken);
            var factory = new MeasurementApiFactory(connection);
            HttpClient? client = null;

            try
            {
                client = factory.CreateClient();
                await using var scope = factory.Services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentDbContext>();
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);

                return new MeasurementApiTestHost(connection, factory, client);
            }
            catch
            {
                client?.Dispose();
                await factory.DisposeAsync();
                await connection.DisposeAsync();
                throw;
            }
        }

        public async Task<Equipment> CreateEquipmentAsync(CancellationToken cancellationToken)
        {
            var equipment = new Equipment(
                "Sensor",
                "Датчик",
                $"SN-{Guid.NewGuid():N}",
                "Test lab");

            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentDbContext>();
            dbContext.Equipments.Add(equipment);
            await dbContext.SaveChangesAsync(cancellationToken);
            return equipment;
        }

        public async Task<Measurement> AddMeasurementAsync(
            Guid equipmentId,
            MeasurementType type,
            double value,
            DateTime timestamp,
            CancellationToken cancellationToken)
        {
            var measurement = new Measurement(equipmentId, type, value, timestamp);
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentDbContext>();
            dbContext.Measurements.Add(measurement);
            await dbContext.SaveChangesAsync(cancellationToken);
            return measurement;
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _factory.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class MeasurementApiFactory(SqliteConnection connection) : WebApplicationFactory<Program>
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
