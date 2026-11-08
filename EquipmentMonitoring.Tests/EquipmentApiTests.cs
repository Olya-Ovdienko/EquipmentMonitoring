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

public class EquipmentApiTests
{
    private const string Endpoint = "/api/v1/equipment";

    [Fact]
    public async Task Post_Valid_Equipment_Returns_Created()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await ApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.PostAsJsonAsync(
            Endpoint,
            CreateValidRequest(),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_Valid_Equipment_Returns_All_Response_Fields_And_Generated_Id()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = CreateValidRequest();
        await using var api = await ApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.PostAsJsonAsync(Endpoint, request, cancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EquipmentResponseDto>(cancellationToken);
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Type, result.Type);
        Assert.Equal(request.SerialNumber, result.SerialNumber);
        Assert.Equal(request.Location, result.Location);
        Assert.False(string.IsNullOrWhiteSpace(result.Status));
        Assert.NotEqual(default, result.CreatedAt);
    }

    [Fact]
    public async Task Post_Valid_Equipment_Persists_It_In_Sqlite()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = CreateValidRequest();
        await using var api = await ApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.PostAsJsonAsync(Endpoint, request, cancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EquipmentResponseDto>(cancellationToken);
        Assert.NotNull(result);

        await using var scope = api.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentDbContext>();
        var storedEquipment = await dbContext.Equipments
            .AsNoTracking()
            .SingleOrDefaultAsync(equipment => equipment.Id == result.Id, cancellationToken);

        Assert.NotNull(storedEquipment);
        Assert.Equal(request.SerialNumber, storedEquipment.SerialNumber);
    }

    [Fact]
    public async Task Post_Empty_Name_Returns_BadRequest_With_Name_Error()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = CreateValidRequest();
        request.Name = string.Empty;
        await using var api = await ApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.PostAsJsonAsync(Endpoint, request, cancellationToken);

        await AssertBadRequestHasFieldsAsync(response, cancellationToken, "Name");
    }

    [Fact]
    public async Task Post_Name_Shorter_Than_Three_Characters_Returns_BadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = CreateValidRequest();
        request.Name = "AB";
        await using var api = await ApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.PostAsJsonAsync(Endpoint, request, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Missing_SerialNumber_Returns_BadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new
        {
            Name = "Датчик",
            Type = "Датчик",
            Location = "Лабораторія"
        };
        await using var api = await ApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.PostAsJsonAsync(Endpoint, request, cancellationToken);

        await AssertBadRequestHasFieldsAsync(response, cancellationToken, "SerialNumber");
    }

    [Fact]
    public async Task Post_Invalid_Type_Returns_BadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = CreateValidRequest();
        request.Type = "Невідомий тип";
        await using var api = await ApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.PostAsJsonAsync(Endpoint, request, cancellationToken);

        await AssertBadRequestHasFieldsAsync(response, cancellationToken, "Type");
    }

    [Fact]
    public async Task Post_Request_With_Multiple_Errors_Returns_All_Field_Names()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new
        {
            Name = string.Empty,
            Type = "Невідомий тип",
            SerialNumber = string.Empty,
            Location = " "
        };
        await using var api = await ApiTestHost.CreateAsync(cancellationToken);

        using var response = await api.Client.PostAsJsonAsync(Endpoint, request, cancellationToken);

        await AssertBadRequestHasFieldsAsync(
            response,
            cancellationToken,
            "Name",
            "Type",
            "SerialNumber",
            "Location");
    }

    [Fact]
    public async Task Post_Duplicate_SerialNumber_Returns_Conflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var api = await ApiTestHost.CreateAsync(cancellationToken);
        var request = CreateValidRequest("SN-DUPLICATE");

        using var firstResponse = await api.Client.PostAsJsonAsync(Endpoint, request, cancellationToken);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var secondResponse = await api.Client.PostAsJsonAsync(Endpoint, request, cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    private static CreateEquipmentDto CreateValidRequest(string serialNumber = "SN-HTTP-001") => new()
    {
        Name = "Датчик",
        Type = "Датчик",
        SerialNumber = serialNumber,
        Location = "Лабораторія"
    };

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
        }
    }

    private sealed class ApiTestHost : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly EquipmentApiFactory _factory;

        private ApiTestHost(SqliteConnection connection, EquipmentApiFactory factory, HttpClient client)
        {
            _connection = connection;
            _factory = factory;
            Client = client;
        }

        public HttpClient Client { get; }
        public IServiceProvider Services => _factory.Services;

        public static async Task<ApiTestHost> CreateAsync(CancellationToken cancellationToken)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(cancellationToken);
            var factory = new EquipmentApiFactory(connection);
            HttpClient? client = null;

            try
            {
                client = factory.CreateClient();
                await using var scope = factory.Services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentDbContext>();
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);

                return new ApiTestHost(connection, factory, client);
            }
            catch
            {
                client?.Dispose();
                await factory.DisposeAsync();
                await connection.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _factory.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class EquipmentApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection;

        public EquipmentApiFactory(SqliteConnection connection)
        {
            _connection = connection;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<EquipmentDbContext>>();
                services.RemoveAll<EquipmentDbContext>();
                services.AddDbContext<EquipmentDbContext>(options => options.UseSqlite(_connection));
            });
        }
    }
}
