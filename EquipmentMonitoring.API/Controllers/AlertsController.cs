using System.Globalization;
using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Validators;
using EquipmentMonitoring.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EquipmentMonitoring.API.Controllers;

/// <summary>Provides endpoints for browsing and acknowledging alerts.</summary>
[ApiController]
[Route("api/v1")]
public sealed class AlertsController(IAlertService alertService) : ControllerBase
{
    /// <summary>Gets alerts matching the optional filters.</summary>
    /// <param name="severity">Optional alert severity.</param>
    /// <param name="isAcknowledged">Optional acknowledgement state.</param>
    /// <param name="fromDate">Optional inclusive UTC lower timestamp boundary.</param>
    /// <param name="toDate">Optional inclusive UTC upper timestamp boundary.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The matching alerts, possibly an empty array.</returns>
    /// <response code="200">The matching alerts were returned.</response>
    /// <response code="400">One or more filters are invalid.</response>
    [HttpGet("alerts")]
    [SwaggerOperation(
        Summary = "Get alerts",
        Description = "Returns alerts filtered by optional severity, acknowledgement state and inclusive UTC date range.",
        OperationId = "GetAlerts",
        Tags = new[] { "Alerts" })]
    [ProducesResponseType(typeof(IReadOnlyList<AlertResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AlertResponseDto>>> Get(
        [FromQuery] AlertSeverity? severity,
        [FromQuery] bool? isAcknowledged,
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        try
        {
            if (!TryParseUtcDate(fromDate, nameof(fromDate), out var parsedFromDate)
                || !TryParseUtcDate(toDate, nameof(toDate), out var parsedToDate))
            {
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            var alerts = await alertService.GetAsync(
                severity,
                isAcknowledged,
                parsedFromDate,
                parsedToDate,
                cancellationToken);

            return Ok(alerts);
        }
        catch (ValidationException exception)
        {
            return BadRequest(ToValidationProblemDetails(exception));
        }
    }

    /// <summary>Gets alerts for an equipment item.</summary>
    /// <param name="equipmentId">Equipment identifier.</param>
    /// <param name="severity">Optional alert severity.</param>
    /// <param name="isAcknowledged">Optional acknowledgement state.</param>
    /// <param name="fromDate">Optional inclusive UTC lower timestamp boundary.</param>
    /// <param name="toDate">Optional inclusive UTC upper timestamp boundary.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The matching alerts, possibly an empty array.</returns>
    /// <response code="200">The matching alerts were returned.</response>
    /// <response code="400">One or more filters are invalid.</response>
    /// <response code="404">The equipment does not exist.</response>
    [HttpGet("equipment/{equipmentId:guid}/alerts")]
    [SwaggerOperation(
        Summary = "Get equipment alerts",
        Description = "Returns alerts for an equipment item, filtered by optional severity, acknowledgement state and inclusive UTC date range.",
        OperationId = "GetEquipmentAlerts",
        Tags = new[] { "Alerts" })]
    [ProducesResponseType(typeof(IReadOnlyList<AlertResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AlertResponseDto>>> GetByEquipmentId(
        Guid equipmentId,
        [FromQuery] AlertSeverity? severity,
        [FromQuery] bool? isAcknowledged,
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        CancellationToken cancellationToken)
    {
        if (equipmentId == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(equipmentId)] = ["EquipmentId must not be empty."]
            }));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        try
        {
            if (!TryParseUtcDate(fromDate, nameof(fromDate), out var parsedFromDate)
                || !TryParseUtcDate(toDate, nameof(toDate), out var parsedToDate))
            {
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            var alerts = await alertService.GetByEquipmentIdAsync(
                equipmentId,
                severity,
                isAcknowledged,
                parsedFromDate,
                parsedToDate,
                cancellationToken);

            return Ok(alerts);
        }
        catch (ValidationException exception) when (IsEquipmentNotFound(exception))
        {
            return EquipmentNotFound();
        }
        catch (ValidationException exception)
        {
            return BadRequest(ToValidationProblemDetails(exception));
        }
    }

    /// <summary>Acknowledges an alert.</summary>
    /// <param name="id">Alert identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The acknowledged alert.</returns>
    /// <response code="200">The alert was acknowledged.</response>
    /// <response code="400">The alert identifier is invalid.</response>
    /// <response code="404">The alert does not exist.</response>
    [HttpPatch("alerts/{id}/acknowledge")]
    [SwaggerOperation(
        Summary = "Acknowledge an alert",
        Description = "Marks an existing alert as acknowledged. Repeated acknowledgement is idempotent.",
        OperationId = "AcknowledgeAlert",
        Tags = new[] { "Alerts" })]
    [ProducesResponseType(typeof(AlertResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponseDto>> Acknowledge(
        string id,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var alertId) || alertId == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(id)] = ["Id must not be empty."]
            }));
        }

        var alert = await alertService.AcknowledgeAsync(alertId, cancellationToken);
        return alert is null
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Alert not found."
            })
            : Ok(alert);
    }

    private bool TryParseUtcDate(string? value, string propertyName, out DateTime? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!value.EndsWith("Z", StringComparison.OrdinalIgnoreCase)
            || !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            ModelState.AddModelError(propertyName, "Date must be a valid UTC timestamp.");
            return false;
        }

        result = parsed.UtcDateTime;
        return true;
    }

    private static bool IsEquipmentNotFound(ValidationException exception)
    {
        return exception.Errors.Count() == 1
            && exception.Errors.Single().ErrorCode == MeasurementValidationErrorCodes.EquipmentNotFound;
    }

    private static ValidationProblemDetails ToValidationProblemDetails(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors);
    }

    private static NotFoundObjectResult EquipmentNotFound()
    {
        return new NotFoundObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Equipment not found."
        });
    }
}
