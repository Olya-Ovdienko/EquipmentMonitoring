using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class AlertSwaggerTests
{
    private const string AlertsPath = "/api/v1/alerts";
    private const string EquipmentAlertsPath = "/api/v1/equipment/{equipmentId}/alerts";
    private const string AcknowledgePath = "/api/v1/alerts/{id}/acknowledge";

    [Fact]
    public async Task SwaggerJson_Contains_Documented_Alert_Operations()
    {
        using var document = await GetSwaggerDocumentAsync();

        var alertsOperation = GetOperation(document.RootElement, AlertsPath, "get");
        var equipmentAlertsOperation = GetOperation(document.RootElement, EquipmentAlertsPath, "get");
        var acknowledgeOperation = GetOperation(document.RootElement, AcknowledgePath, "patch");

        AssertHasDescription(alertsOperation);
        AssertHasDescription(equipmentAlertsOperation);
        AssertHasDescription(acknowledgeOperation);
    }

    [Fact]
    public async Task SwaggerJson_Alert_Operations_Document_Expected_Response_Codes()
    {
        using var document = await GetSwaggerDocumentAsync();

        var alertsOperation = GetOperation(document.RootElement, AlertsPath, "get");
        var equipmentAlertsOperation = GetOperation(document.RootElement, EquipmentAlertsPath, "get");
        var acknowledgeOperation = GetOperation(document.RootElement, AcknowledgePath, "patch");

        AssertResponseExists(alertsOperation, "200");
        AssertResponseExists(alertsOperation, "400");

        AssertResponseExists(equipmentAlertsOperation, "200");
        AssertResponseExists(equipmentAlertsOperation, "400");
        AssertResponseExists(equipmentAlertsOperation, "404");

        AssertResponseExists(acknowledgeOperation, "200");
        AssertResponseExists(acknowledgeOperation, "400");
        AssertResponseExists(acknowledgeOperation, "404");
    }

    [Fact]
    public async Task SwaggerJson_Get_Operations_Document_All_Optional_Filters_And_Route_Ids()
    {
        using var document = await GetSwaggerDocumentAsync();
        var alertsParameters = GetOperation(document.RootElement, AlertsPath, "get")
            .GetProperty("parameters").EnumerateArray().ToArray();
        var equipmentParameters = GetOperation(document.RootElement, EquipmentAlertsPath, "get")
            .GetProperty("parameters").EnumerateArray().ToArray();
        var acknowledgeParameters = GetOperation(document.RootElement, AcknowledgePath, "patch")
            .GetProperty("parameters").EnumerateArray().ToArray();

        AssertOptionalQueryParameter(alertsParameters, "severity");
        AssertOptionalQueryParameter(alertsParameters, "isAcknowledged");
        AssertOptionalQueryParameter(alertsParameters, "fromDate");
        AssertOptionalQueryParameter(alertsParameters, "toDate");

        AssertRouteGuidParameter(equipmentParameters, "equipmentId");
        AssertOptionalQueryParameter(equipmentParameters, "severity");
        AssertOptionalQueryParameter(equipmentParameters, "isAcknowledged");
        AssertOptionalQueryParameter(equipmentParameters, "fromDate");
        AssertOptionalQueryParameter(equipmentParameters, "toDate");

        AssertRouteGuidParameter(acknowledgeParameters, "id");
    }

    [Fact]
    public async Task SwaggerJson_Get_Responses_Use_Alert_Response_Array_And_Patch_Uses_Alert_Response()
    {
        using var document = await GetSwaggerDocumentAsync();
        var alertsOperation = GetOperation(document.RootElement, AlertsPath, "get");
        var equipmentAlertsOperation = GetOperation(document.RootElement, EquipmentAlertsPath, "get");
        var acknowledgeOperation = GetOperation(document.RootElement, AcknowledgePath, "patch");

        AssertAlertArrayResponse(document.RootElement, alertsOperation, "200");
        AssertAlertArrayResponse(document.RootElement, equipmentAlertsOperation, "200");
        AssertAlertResponse(document.RootElement, acknowledgeOperation, "200");
    }

    [Fact]
    public async Task SwaggerJson_AlertResponseSchema_Documents_All_Fields()
    {
        using var document = await GetSwaggerDocumentAsync();
        var schema = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("AlertResponseDto");
        var properties = schema.GetProperty("properties");

        foreach (var propertyName in new[]
        {
            "id",
            "equipmentId",
            "measurementId",
            "message",
            "severity",
            "timestamp",
            "isAcknowledged"
        })
        {
            Assert.True(properties.TryGetProperty(propertyName, out var property), $"Missing property {propertyName}.");
            AssertHasMeaningfulDescription(property);
        }
    }

    [Fact]
    public async Task SwaggerJson_Alert_Operations_Contain_Success_Validation_And_NotFound_Examples()
    {
        using var document = await GetSwaggerDocumentAsync();
        var alertsOperation = GetOperation(document.RootElement, AlertsPath, "get");
        var equipmentAlertsOperation = GetOperation(document.RootElement, EquipmentAlertsPath, "get");
        var acknowledgeOperation = GetOperation(document.RootElement, AcknowledgePath, "patch");

        AssertAlertArrayExample(alertsOperation, "200");
        AssertProblemExample(alertsOperation, "400", expectedStatus: 400);

        AssertAlertArrayExample(equipmentAlertsOperation, "200");
        AssertProblemExample(equipmentAlertsOperation, "400", expectedStatus: 400);
        AssertProblemExample(equipmentAlertsOperation, "404", expectedStatus: 404);

        AssertAlertExample(acknowledgeOperation, "200");
        AssertProblemExample(acknowledgeOperation, "400", expectedStatus: 400);
        AssertProblemExample(acknowledgeOperation, "404", expectedStatus: 404);
    }

    private static async Task<JsonDocument> GetSwaggerDocumentAsync()
    {
        await using var factory = new SwaggerApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(
            "/swagger/v1/swagger.json",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
    }

    private static JsonElement GetOperation(JsonElement document, string path, string method)
    {
        return document.GetProperty("paths").GetProperty(path).GetProperty(method);
    }

    private static void AssertOptionalQueryParameter(JsonElement[] parameters, string name)
    {
        var parameter = Assert.Single(parameters, item => item.GetProperty("name").GetString() == name);
        Assert.Equal("query", parameter.GetProperty("in").GetString());
        var required = parameter.TryGetProperty("required", out var requiredValue) && requiredValue.GetBoolean();
        Assert.False(required);
    }

    private static void AssertRouteGuidParameter(JsonElement[] parameters, string name)
    {
        var parameter = Assert.Single(parameters, item => item.GetProperty("name").GetString() == name);
        Assert.Equal("path", parameter.GetProperty("in").GetString());
        Assert.True(parameter.GetProperty("required").GetBoolean());
        var schema = parameter.GetProperty("schema");
        Assert.True(schema.TryGetProperty("type", out var type));
        Assert.Equal("string", type.GetString());
        Assert.True(schema.TryGetProperty("format", out var format));
        Assert.Equal("uuid", format.GetString());
    }

    private static void AssertAlertArrayResponse(JsonElement document, JsonElement operation, string statusCode)
    {
        var schema = operation.GetProperty("responses").GetProperty(statusCode)
            .GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Assert.Equal("array", schema.GetProperty("type").GetString());
        var itemSchema = ResolveSchema(document, schema.GetProperty("items"));
        AssertAlertSchema(document, itemSchema);
    }

    private static void AssertAlertResponse(JsonElement document, JsonElement operation, string statusCode)
    {
        var schema = operation.GetProperty("responses").GetProperty(statusCode)
            .GetProperty("content").GetProperty("application/json").GetProperty("schema");
        var resolved = ResolveSchema(document, schema);
        AssertAlertSchema(document, resolved);
    }

    private static void AssertAlertExample(JsonElement operation, string statusCode)
    {
        var example = GetResponseExample(operation, statusCode);
        Assert.Equal(JsonValueKind.Object, example.ValueKind);
        Assert.True(Guid.TryParse(example.GetProperty("id").GetString(), out _));
        Assert.True(Guid.TryParse(example.GetProperty("equipmentId").GetString(), out _));
        Assert.True(Guid.TryParse(example.GetProperty("measurementId").GetString(), out _));
        Assert.False(string.IsNullOrWhiteSpace(example.GetProperty("message").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(example.GetProperty("severity").GetString()));
        Assert.True(DateTimeOffset.TryParse(example.GetProperty("timestamp").GetString(), out _));
        Assert.Equal(JsonValueKind.True, example.GetProperty("isAcknowledged").ValueKind);
    }

    private static void AssertAlertArrayExample(JsonElement operation, string statusCode)
    {
        var example = GetResponseExample(operation, statusCode);
        Assert.Equal(JsonValueKind.Array, example.ValueKind);
        Assert.NotEmpty(example.EnumerateArray());
        AssertAlertExampleElement(example[0]);
    }

    private static void AssertAlertExampleElement(JsonElement example)
    {
        Assert.Equal(JsonValueKind.Object, example.ValueKind);
        Assert.True(Guid.TryParse(example.GetProperty("id").GetString(), out _));
        Assert.False(string.IsNullOrWhiteSpace(example.GetProperty("severity").GetString()));
        Assert.True(DateTimeOffset.TryParse(example.GetProperty("timestamp").GetString(), out _));
    }

    private static void AssertProblemExample(JsonElement operation, string statusCode, int expectedStatus)
    {
        var example = GetResponseExample(operation, statusCode);
        Assert.Equal(JsonValueKind.Object, example.ValueKind);
        Assert.Equal(expectedStatus, example.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(example.GetProperty("title").GetString()));
        if (expectedStatus == 400)
        {
            Assert.True(example.TryGetProperty("errors", out var errors));
            Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        }
    }

    private static JsonElement ResolveSchema(JsonElement document, JsonElement schema)
    {
        if (!schema.TryGetProperty("$ref", out var reference))
        {
            return schema;
        }

        var schemaName = reference.GetString()!.Split('/')[^1];
        return document.GetProperty("components").GetProperty("schemas").GetProperty(schemaName);
    }

    private static void AssertHasDescription(JsonElement operation)
    {
        Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
    }

    private static void AssertHasMeaningfulDescription(JsonElement property)
    {
        Assert.True(property.TryGetProperty("description", out var description));
        Assert.False(string.IsNullOrWhiteSpace(description.GetString()));
    }

    private static void AssertAlertSchema(JsonElement document, JsonElement schema)
    {
        if (schema.TryGetProperty("$ref", out var reference))
        {
            Assert.EndsWith("/AlertResponseDto", reference.GetString());
            return;
        }

        Assert.True(schema.TryGetProperty("properties", out var properties));
        Assert.True(properties.TryGetProperty("id", out _));
        Assert.True(properties.TryGetProperty("isAcknowledged", out _));
    }

    private static void AssertResponseExists(JsonElement operation, string statusCode)
    {
        Assert.True(operation.GetProperty("responses").TryGetProperty(statusCode, out _));
    }

    private static JsonElement GetResponseExample(JsonElement operation, string statusCode)
    {
        var content = operation.GetProperty("responses").GetProperty(statusCode)
            .GetProperty("content").GetProperty("application/json");
        Assert.True(content.TryGetProperty("example", out var example), "Expected a JSON example in OpenAPI.");
        return example;
    }

    private sealed class SwaggerApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
        }
    }
}
