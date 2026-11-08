using System.Text.Json.Nodes;
using EquipmentMonitoring.Domain;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EquipmentMonitoring.API.Swagger;

public sealed class MeasurementExamplesOperationFilter : IOperationFilter, ISchemaFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath?.Trim('/');
        var method = context.ApiDescription.HttpMethod;

        if (path == "api/v1/measurements" && string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            SetRequestExample(operation, """
                {
                  "equipmentId": "2f23e6f4-5c0d-4c63-9e88-69f12c1fc491",
                  "type": 0,
                  "value": 21.5,
                  "timestamp": "2026-01-15T10:30:00Z"
                }
                """);
            SetResponseExample(operation, "201", """
                {
                  "id": "5f5f2d89-4e8d-4c67-b7c1-3a4e7a5a9c10",
                  "equipmentId": "2f23e6f4-5c0d-4c63-9e88-69f12c1fc491",
                  "type": "Temperature",
                  "value": 21.5,
                  "unit": "°C",
                  "timestamp": "2026-01-15T10:30:00Z"
                }
                """);
            SetResponseExample(operation, "400", """
                {
                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                  "title": "One or more validation errors occurred.",
                  "status": 400,
                  "errors": {
                    "Value": ["Value must be finite and non-negative for this measurement type."]
                  }
                }
                """);
            SetResponseExample(operation, "404", """
                {
                  "title": "Equipment not found.",
                  "status": 404
                }
                """);
            return;
        }

        if (path == "api/v1/equipment/{equipmentId}/measurements"
            || path == "api/v1/equipment/{equipmentId:guid}/measurements")
        {
            if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SetResponseExample(operation, "200", """
                [
                  {
                    "id": "5f5f2d89-4e8d-4c67-b7c1-3a4e7a5a9c10",
                    "equipmentId": "2f23e6f4-5c0d-4c63-9e88-69f12c1fc491",
                    "type": "Temperature",
                    "value": 21.5,
                    "unit": "°C",
                    "timestamp": "2026-01-15T10:30:00Z"
                  },
                  {
                    "id": "69c5df5a-8e21-4c88-bd26-2fa071d07e81",
                    "equipmentId": "2f23e6f4-5c0d-4c63-9e88-69f12c1fc491",
                    "type": "Voltage",
                    "value": 230,
                    "unit": "V",
                    "timestamp": "2026-01-15T10:35:00Z"
                  }
                ]
                """);
            SetResponseExample(operation, "404", """
                {
                  "title": "Equipment not found.",
                  "status": 404
                }
                """);
        }
    }

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(MeasurementType) && schema is OpenApiSchema measurementTypeSchema)
        {
            measurementTypeSchema.Description = "Measurement kind encoded as a numeric JSON enum: Temperature (0), Voltage (1), or Load (2).";
        }
    }

    private static void SetRequestExample(OpenApiOperation operation, string json)
    {
        if (operation.RequestBody?.Content?.TryGetValue("application/json", out var content) == true && content is not null)
        {
            content.Example = JsonNode.Parse(json);
        }
    }

    private static void SetResponseExample(OpenApiOperation operation, string statusCode, string json)
    {
        if (operation.Responses?.TryGetValue(statusCode, out var response) == true
            && response?.Content?.TryGetValue("application/json", out var content) == true
            && content is not null)
        {
            content.Example = JsonNode.Parse(json);
        }
    }
}
