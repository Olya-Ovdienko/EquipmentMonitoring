using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentMonitoring.API.Controllers;

[ApiController]
[Route("api/v1/equipment")]
public sealed class EquipmentController(IEquipmentRegistrationService registrationService) : ControllerBase
{
    [HttpPost]
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
