using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class MeasurementSwaggerTests
{
    private const string CreateMeasurementPath = "/api/v1/measurements";
    private const string MeasurementHistoryPath = "/api/v1/equipment/{equipmentId}/measurements";

    [Fact]
    public async Task SwaggerJson_ReturnsOkAndValidOpenApiDocument()
    {
        // Arrange
        using var document = await GetSwaggerDocumentAsync();

        // Assert
        Assert.True(document.RootElement.TryGetProperty("openapi", out var version));
        Assert.StartsWith("3.", version.GetString());
        Assert.True(document.RootElement.TryGetProperty("paths", out _));
    }

    [Fact]
    public async Task SwaggerJson_ContainsDocumentedMeasurementOperations()
    {
        // Arrange
        using var document = await GetSwaggerDocumentAsync();
        var createOperation = GetOperation(document.RootElement, CreateMeasurementPath, "post");
        var historyOperation = GetOperation(document.RootElement, MeasurementHistoryPath, "get");

        // Assert
        AssertHasDescription(createOperation);
        AssertHasDescription(historyOperation);
    }

    [Fact]
    public async Task SwaggerJson_PostDocumentsCreatedBadRequestAndNotFoundResponses()
    {
        // Arrange
        using var document = await GetSwaggerDocumentAsync();
        var operation = GetOperation(document.RootElement, CreateMeasurementPath, "post");

        // Assert
        AssertResponseExists(operation, "201");
        AssertResponseExists(operation, "400");
        AssertResponseExists(operation, "404");
    }

    [Fact]
    public async Task SwaggerJson_GetDocumentsOkAndNotFoundResponses()
    {
        // Arrange
        using var document = await GetSwaggerDocumentAsync();
        var operation = GetOperation(document.RootElement, MeasurementHistoryPath, "get");

        // Assert
        AssertResponseExists(operation, "200");
        AssertResponseExists(operation, "404");
    }

    [Fact]
    public async Task SwaggerJson_GetDocumentsEquipmentIdAndOptionalFilters()
    {
        // Arrange
        using var document = await GetSwaggerDocumentAsync();
        var operation = GetOperation(document.RootElement, MeasurementHistoryPath, "get");
        var parameters = operation.GetProperty("parameters").EnumerateArray().ToArray();

        // Assert
        var equipmentId = AssertParameter(parameters, "equipmentId", "path", required: true);
        var equipmentIdSchema = ResolveSchema(document.RootElement, equipmentId.GetProperty("schema"));
        Assert.Equal("string", equipmentIdSchema.GetProperty("type").GetString());
        Assert.Equal("uuid", equipmentIdSchema.GetProperty("format").GetString());
        AssertParameter(parameters, "type", "query", required: false);
        AssertParameter(parameters, "startDate", "query", required: false);
        AssertParameter(parameters, "endDate", "query", required: false);
    }

    [Fact]
    public async Task SwaggerJson_CreateMeasurementSchemaDocumentsFieldsAndValidationRequirements()
    {
        // Arrange
        using var document = await GetSwaggerDocumentAsync();
        var operation = GetOperation(document.RootElement, CreateMeasurementPath, "post");
        var schema = ResolveSchema(document.RootElement, operation
            .GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema"));
        var properties = schema.GetProperty("properties");
        var required = schema.TryGetProperty("required", out var requiredFields)
            ? requiredFields.EnumerateArray().Select(item => item.GetString()).ToArray()
            : [];

        // Assert
        Assert.Contains("equipmentId", required);
        Assert.Equal("string", properties.GetProperty("equipmentId").GetProperty("type").GetString());
        Assert.Equal("uuid", properties.GetProperty("equipmentId").GetProperty("format").GetString());
        Assert.Contains("type", required);

        var typeSchema = ResolveSchema(document.RootElement, properties.GetProperty("type"));
        Assert.Equal("integer", typeSchema.GetProperty("type").GetString());
        var enumValues = typeSchema.GetProperty("enum").EnumerateArray()
            .Select(item => item.GetInt32())
            .Order()
            .ToArray();
        Assert.Equal(new[] { 0, 1, 2 }, enumValues);

        var valueSchema = properties.GetProperty("value");
        Assert.Equal("number", valueSchema.GetProperty("type").GetString());
        Assert.DoesNotContain("value", required);
        Assert.DoesNotContain("timestamp", required);
        Assert.Equal("string", properties.GetProperty("timestamp").GetProperty("type").GetString());
        Assert.Equal("date-time", properties.GetProperty("timestamp").GetProperty("format").GetString());

        AssertHasMeaningfulDescription(properties.GetProperty("equipmentId"));
        AssertHasMeaningfulDescription(typeSchema);
        AssertHasMeaningfulDescription(valueSchema);
        AssertHasMeaningfulDescription(properties.GetProperty("timestamp"));
    }

    [Fact]
    public async Task SwaggerJson_MeasurementResponseSchemaDocumentsEveryField()
    {
        // Arrange
        using var document = await GetSwaggerDocumentAsync();
        var operation = GetOperation(document.RootElement, CreateMeasurementPath, "post");
        var responseSchema = ResolveSchema(
            document.RootElement,
            operation.GetProperty("responses").GetProperty("201")
                .GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        var properties = responseSchema.GetProperty("properties");

        // Assert
        foreach (var propertyName in new[] { "id", "equipmentId", "type", "value", "unit", "timestamp" })
        {
            AssertHasMeaningfulDescription(properties.GetProperty(propertyName));
        }
    }

    [Fact]
    public async Task SwaggerJson_PostContainsValidRequestAndSuccessResponseExamples()
    {
        // Arrange
        using var document = await GetSwaggerDocumentAsync();
        var operation = GetOperation(document.RootElement, CreateMeasurementPath, "post");

        // Assert
        var requestExample = GetMediaTypeExample(
            operation.GetProperty("requestBody").GetProperty("content").GetProperty("application/json"));
        Assert.Equal(JsonValueKind.Object, requestExample.ValueKind);
        Assert.True(Guid.TryParse(requestExample.GetProperty("equipmentId").GetString(), out _));
        Assert.Equal(0, requestExample.GetProperty("type").GetInt32());
        Assert.True(requestExample.GetProperty("value").GetDouble() >= 0);
        Assert.True(DateTimeOffset.TryParse(requestExample.GetProperty("timestamp").GetString(), out _));

        var responseExample = GetResponseExample(operation, "201");
        AssertMeasurementResponseExample(responseExample);
    }

    [Fact]
    public async Task SwaggerJson_PostContainsValidBadRequestAndNotFoundExamples()
    {
        // Arrange
        using var document = await GetSwaggerDocumentAsync();
        var operation = GetOperation(document.RootElement, CreateMeasurementPath, "post");

        // Assert
        var badRequest = GetResponseExample(operation, "400");
        Assert.Equal(JsonValueKind.Object, badRequest.ValueKind);
        Assert.True(badRequest.TryGetProperty("errors", out var errors));
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        Assert.Equal(400, badRequest.GetProperty("status").GetInt32());

        var notFound = GetResponseExample(operation, "404");
        Assert.Equal(JsonValueKind.Object, notFound.ValueKind);
        Assert.Equal(404, notFound.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(notFound.GetProperty("title").GetString()));
    }

    [Fact]
    public async Task SwaggerJson_GetContainsValidHistoryAndNotFoundExamples()
    {
        // Arrange
        using var document = await GetSwaggerDocumentAsync();
        var operation = GetOperation(document.RootElement, MeasurementHistoryPath, "get");

        // Assert
        var historyContent = operation.GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json");
        var historyExample = GetMediaTypeExample(historyContent);
        Assert.Equal(JsonValueKind.Array, historyExample.ValueKind);
        Assert.NotEmpty(historyExample.EnumerateArray());
        AssertMeasurementResponseExample(historyExample[0]);

        var notFound = GetResponseExample(operation, "404");
        Assert.Equal(JsonValueKind.Object, notFound.ValueKind);
        Assert.Equal(404, notFound.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(notFound.GetProperty("title").GetString()));
    }

    private static async Task<JsonDocument> GetSwaggerDocumentAsync()
    {
        await using var factory = new SwaggerApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
    }

    private static JsonElement GetOperation(JsonElement document, string path, string method)
    {
        return document.GetProperty("paths").GetProperty(path).GetProperty(method);
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

    private static JsonElement AssertParameter(
        JsonElement[] parameters,
        string name,
        string location,
        bool required)
    {
        var parameter = Assert.Single(parameters, item => item.GetProperty("name").GetString() == name);
        Assert.Equal(location, parameter.GetProperty("in").GetString());
        var isRequired = parameter.TryGetProperty("required", out var requiredValue)
            && requiredValue.GetBoolean();
        Assert.Equal(required, isRequired);
        return parameter;
    }

    private static void AssertHasDescription(JsonElement operation)
    {
        Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
    }

    private static void AssertResponseExists(JsonElement operation, string statusCode)
    {
        Assert.True(operation.GetProperty("responses").TryGetProperty(statusCode, out _));
    }

    private static JsonElement GetResponseExample(JsonElement operation, string statusCode)
    {
        var content = operation.GetProperty("responses").GetProperty(statusCode)
            .GetProperty("content").GetProperty("application/json");
        return GetMediaTypeExample(content);
    }

    private static JsonElement GetMediaTypeExample(JsonElement mediaType)
    {
        Assert.True(mediaType.TryGetProperty("example", out var example), "Expected a JSON example in OpenAPI.");
        return example;
    }

    private static void AssertMeasurementResponseExample(JsonElement example)
    {
        Assert.Equal(JsonValueKind.Object, example.ValueKind);
        Assert.True(Guid.TryParse(example.GetProperty("id").GetString(), out _));
        Assert.True(Guid.TryParse(example.GetProperty("equipmentId").GetString(), out _));
        Assert.False(string.IsNullOrWhiteSpace(example.GetProperty("type").GetString()));
        Assert.True(example.GetProperty("value").ValueKind == JsonValueKind.Number);
        Assert.False(string.IsNullOrWhiteSpace(example.GetProperty("unit").GetString()));
        Assert.True(DateTimeOffset.TryParse(example.GetProperty("timestamp").GetString(), out _));
    }

    private static void AssertHasMeaningfulDescription(JsonElement schema)
    {
        Assert.True(schema.TryGetProperty("description", out var description));
        Assert.True(description.GetString()?.Length >= 10);
    }

    private sealed class SwaggerApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
        }
    }
}
