using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Validators;
using EquipmentMonitoring.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EquipmentMonitoring.API.Controllers;

/// <summary>Provides endpoints for creating and browsing equipment measurements.</summary>
[ApiController]
[Route("api/v1")]
public sealed class MeasurementsController(IMeasurementService measurementService) : ControllerBase
{
    /// <summary>Creates a measurement for an existing equipment item.</summary>
    /// <param name="request">Measurement values and the associated equipment identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The created measurement.</returns>
    /// <response code="201">The measurement was created.</response>
    /// <response code="400">One or more measurement fields are invalid.</response>
    /// <response code="404">The specified equipment does not exist.</response>
    [HttpPost("measurements")]
    [SwaggerOperation(
        Summary = "Create a measurement",
        Description = "Validates and stores a measurement for an existing equipment item.",
        OperationId = "CreateMeasurement",
        Tags = new[] { "Measurements" })]
    [ProducesResponseType(typeof(MeasurementResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeasurementResponseDto>> Create(
        [FromBody] CreateMeasurementDto request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["Request"] = ["A valid request body is required."]
            }));
        }

        try
        {
            var response = await measurementService.CreateAsync(request, cancellationToken);
            return Created($"/api/v1/equipment/{response.EquipmentId}/measurements/{response.Id}", response);
        }
        catch (ValidationException exception)
        {
            if (IsEquipmentNotFound(exception))
            {
                return EquipmentNotFound();
            }

            return BadRequest(ToValidationProblemDetails(exception));
        }
    }

    /// <summary>Gets the measurement history for an equipment item.</summary>
    /// <param name="equipmentId">The equipment identifier.</param>
    /// <param name="type">Optional measurement type filter.</param>
    /// <param name="startDate">Optional inclusive UTC start date.</param>
    /// <param name="endDate">Optional inclusive UTC end date.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The matching measurements, or an empty list.</returns>
    /// <response code="200">The history was returned, possibly empty.</response>
    /// <response code="404">The specified equipment does not exist.</response>
    [HttpGet("equipment/{equipmentId:guid}/measurements")]
    [SwaggerOperation(
        Summary = "Get measurement history",
        Description = "Returns measurements for an equipment item, optionally filtered by type and inclusive UTC date range.",
        OperationId = "GetMeasurementHistory",
        Tags = new[] { "Measurements" })]
    [ProducesResponseType(typeof(IReadOnlyList<MeasurementResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MeasurementResponseDto>>> GetByEquipmentId(
        Guid equipmentId,
        [FromQuery] MeasurementType? type,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var measurements = await measurementService.GetByEquipmentIdAsync(
                equipmentId,
                type,
                startDate,
                endDate,
                cancellationToken);

            return Ok(measurements);
        }
        catch (ValidationException exception) when (IsEquipmentNotFound(exception))
        {
            return EquipmentNotFound();
        }
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
