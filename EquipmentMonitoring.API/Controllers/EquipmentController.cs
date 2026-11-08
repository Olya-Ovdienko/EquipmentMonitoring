using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EquipmentMonitoring.API.Controllers;

/// <summary>Provides HTTP endpoints for registering equipment.</summary>
/// <remarks>Equipment is validated and persisted through the application service.</remarks>
[ApiController]
[Route("api/v1/equipment")]
public sealed class EquipmentController(IEquipmentRegistrationService registrationService) : ControllerBase
{
    /// <summary>Registers a new equipment item.</summary>
    /// <remarks>
    /// Accepts a JSON <see cref="CreateEquipmentDto"/>. Name is required and must contain 3–100
    /// characters; Type is required and must be Датчик, Контролер, or Сервер; SerialNumber is
    /// required and unique; Location is required. Returns the created equipment, field validation
    /// errors, or a conflict when the serial number is already registered.
    /// </remarks>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Register equipment",
        Description = "Validates and stores a new equipment item. Name must contain 3–100 characters; Type must be Датчик, Контролер, or Сервер; SerialNumber must be unique; Location is required.",
        OperationId = "RegisterEquipment",
        Tags = new[] { "Equipment" })]
    [ProducesResponseType(typeof(EquipmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EquipmentResponseDto>> Create(
        [FromBody] CreateEquipmentDto request,
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
            var response = await registrationService.RegisterAsync(request, cancellationToken);
            return Created($"/api/v1/equipment/{response.Id}", response);
        }
        catch (DuplicateEquipmentSerialNumberException)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Equipment serial number already exists."
            });
        }
        catch (ValidationException exception)
        {
            var errors = exception.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).ToArray());

            return BadRequest(new ValidationProblemDetails(errors));
        }
    }
}
