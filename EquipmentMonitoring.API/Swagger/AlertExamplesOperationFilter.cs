using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EquipmentMonitoring.API.Swagger;

public sealed class AlertExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath?.Trim('/');
        var method = context.ApiDescription.HttpMethod;

        if (path == "api/v1/equipment/{equipmentId}/alerts"
            || path == "api/v1/equipment/{equipmentId:guid}/alerts"
            || path == "api/v1/alerts/{id}/acknowledge")
        {
            SetRouteGuidSchema(operation, "equipmentId");
            SetRouteGuidSchema(operation, "id");
        }

        if (path == "api/v1/alerts" && string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            SetResponseExample(operation, "200", """
                [
                  {
                    "id": "5f5f2d89-4e8d-4c67-b7c1-3a4e7a5a9c10",
                    "equipmentId": "2f23e6f4-5c0d-4c63-9e88-69f12c1fc491",
                    "measurementId": "69c5df5a-8e21-4c88-bd26-2fa071d07e81",
                    "message": "Temperature value 95 is above maximum threshold 90.",
                    "severity": "Critical",
                    "timestamp": "2026-01-15T10:30:00Z",
                    "isAcknowledged": false
                  }
                ]
                """);
            SetProblemExamples(operation, includeNotFound: false);
            return;
        }

        if ((path == "api/v1/equipment/{equipmentId}/alerts"
            || path == "api/v1/equipment/{equipmentId:guid}/alerts")
            && string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            SetResponseExample(operation, "200", """
                [
                  {
                    "id": "5f5f2d89-4e8d-4c67-b7c1-3a4e7a5a9c10",
                    "equipmentId": "2f23e6f4-5c0d-4c63-9e88-69f12c1fc491",
                    "measurementId": "69c5df5a-8e21-4c88-bd26-2fa071d07e81",
                    "message": "Temperature value 95 is above maximum threshold 90.",
                    "severity": "Critical",
                    "timestamp": "2026-01-15T10:30:00Z",
                    "isAcknowledged": false
                  }
                ]
                """);
            SetProblemExamples(operation, includeNotFound: true);
            return;
        }

        if (path == "api/v1/alerts/{id}/acknowledge"
            && string.Equals(method, "PATCH", StringComparison.OrdinalIgnoreCase))
        {
            SetResponseExample(operation, "200", """
                {
                  "id": "5f5f2d89-4e8d-4c67-b7c1-3a4e7a5a9c10",
                  "equipmentId": "2f23e6f4-5c0d-4c63-9e88-69f12c1fc491",
                  "measurementId": "69c5df5a-8e21-4c88-bd26-2fa071d07e81",
                  "message": "Temperature value 95 is above maximum threshold 90.",
                  "severity": "Critical",
                  "timestamp": "2026-01-15T10:30:00Z",
                  "isAcknowledged": true
                }
                """);
            SetProblemExamples(operation, includeNotFound: true);
        }

    }

    private static void SetProblemExamples(OpenApiOperation operation, bool includeNotFound)
    {
        SetResponseExample(operation, "400", """
            {
              "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
              "title": "One or more validation errors occurred.",
              "status": 400,
              "errors": {
                "severity": ["Severity is not supported."]
              }
            }
            """);

        if (includeNotFound)
        {
            SetResponseExample(operation, "404", """
                {
                  "title": "Alert or equipment not found.",
                  "status": 404
                }
                """);
        }
    }

    private static void SetRouteGuidSchema(OpenApiOperation operation, string name)
    {
        if (operation.Parameters?.FirstOrDefault(parameter => parameter.Name == name) is { } parameter
            && parameter.Schema is OpenApiSchema schema)
        {
            schema.Format = "uuid";
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
