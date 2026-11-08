using System.Text.Json.Nodes;
using EquipmentMonitoring.Application.DTOs;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EquipmentMonitoring.API.Swagger;

public sealed class EquipmentExamplesOperationFilter : IOperationFilter, ISchemaFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.RelativePath?.Trim('/') != "api/v1/equipment"
            || !string.Equals(context.ApiDescription.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var requestBody = operation.RequestBody;
        if (requestBody is not null
            && requestBody.Content is not null
            && requestBody.Content.TryGetValue("application/json", out var requestContent)
            && requestContent is not null)
        {
            requestContent.Example = JsonNode.Parse("""
                {
                  "name": "Temperature Sensor",
                  "type": "Датчик",
                  "serialNumber": "DOC-001",
                  "location": "Laboratory 1"
                }
                """);
        }

        SetResponseExample(operation, "201", """
            {
              "id": "5f5f2d89-4e8d-4c67-b7c1-3a4e7a5a9c10",
              "name": "Temperature Sensor",
              "type": "Датчик",
              "serialNumber": "DOC-001",
              "location": "Laboratory 1",
              "status": "Available",
              "createdAt": "2026-01-15T10:30:00Z"
            }
            """);
        SetResponseExample(operation, "400", """
            {
              "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
              "title": "One or more validation errors occurred.",
              "status": 400,
              "errors": {
                "name": ["Name is required."],
                "type": ["Type is not supported."]
              }
            }
            """);
        SetResponseExample(operation, "409", """
            {
              "title": "Equipment serial number already exists.",
              "status": 409
            }
            """);
    }

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(CreateEquipmentDto) || schema is not OpenApiSchema requestSchema)
        {
            return;
        }

        requestSchema.Required = new HashSet<string>(["name", "type", "serialNumber", "location"]);

        if (requestSchema.Properties?.TryGetValue("name", out var nameSchema) == true
            && nameSchema is OpenApiSchema mutableNameSchema)
        {
            mutableNameSchema.MinLength = 3;
            mutableNameSchema.MaxLength = 100;
        }

        if (requestSchema.Properties?.TryGetValue("type", out var typeSchema) == true
            && typeSchema is OpenApiSchema mutableTypeSchema)
        {
            mutableTypeSchema.Enum = [
                CreateJsonString("Датчик"),
                CreateJsonString("Контролер"),
                CreateJsonString("Сервер")];
        }
    }

    private static void SetResponseExample(OpenApiOperation operation, string statusCode, string json)
    {
        if (operation.Responses?.TryGetValue(statusCode, out var response) == true
            && response is not null
            && response.Content?.TryGetValue("application/json", out var content) == true
            && content is not null)
        {
            content.Example = JsonNode.Parse(json);
        }
    }

    private static JsonNode CreateJsonString(string value)
    {
        return JsonValue.Create(value) ?? throw new InvalidOperationException("Could not create a JSON string value.");
    }
}
