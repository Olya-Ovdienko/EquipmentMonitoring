using AutoMapper;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.API.Controllers;

/// <summary>
/// Controller for managing equipment maintenance scheduling and logging.
/// </summary>
[ApiController]
[Route("api/v1/maintenances")]
[Produces("application/json")]
public class MaintenancesController : ControllerBase
{
    private readonly EquipmentDbContext _context;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateMaintenanceDto> _createValidator;
    private readonly IValidator<UpdateMaintenanceDto> _updateValidator;

    public MaintenancesController(
        EquipmentDbContext context,
        IMapper mapper,
        IValidator<CreateMaintenanceDto> createValidator,
        IValidator<UpdateMaintenanceDto> updateValidator)
    {
        _context = context;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Create a new maintenance record.
    /// </summary>
    /// <remarks>
    /// Creates a new maintenance schedule for existing equipment. ScheduledDate must be in the future.
    /// </remarks>
    /// <param name="dto">Data for creating a maintenance record.</param>
    /// <returns>Created maintenance record.</returns>
    /// <response code="201">Maintenance record successfully created.</response>
    /// <response code="400">Input validation error (e.g. past date or empty description).</response>
    /// <response code="404">Equipment with specified EquipmentId not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MaintenanceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MaintenanceResponseDto>> Create([FromBody] CreateMaintenanceDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == dto.EquipmentId);
        if (equipment == null || dto.EquipmentId == Guid.Empty)
        {
            return NotFound($"Equipment with Id {dto.EquipmentId} not found.");
        }

        var maintenance = _mapper.Map<Maintenance>(dto);
        maintenance.Equipment = equipment;

        _context.Maintenances.Add(maintenance);
        await _context.SaveChangesAsync();

        var response = _mapper.Map<MaintenanceResponseDto>(maintenance);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>
    /// Get list of maintenance records with optional filtering.
    /// </summary>
    /// <remarks>
    /// Returns a list of all maintenance records. Supports filtering by equipment, status, and date range.
    /// </remarks>
    /// <param name="equipmentId">Optional filter by equipment ID.</param>
    /// <param name="status">Optional filter by status (Scheduled, InProgress, Completed, Cancelled).</param>
    /// <param name="fromDate">Start date for filtering scheduledDate.</param>
    /// <param name="toDate">End date for filtering scheduledDate.</param>
    /// <returns>List of matching maintenance records.</returns>
    /// <response code="200">Successfully retrieved list.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MaintenanceResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<MaintenanceResponseDto>>> GetAll(
        [FromQuery] Guid? equipmentId,
        [FromQuery] MaintenanceStatus? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var query = _context.Maintenances.AsQueryable();

        if (equipmentId.HasValue)
        {
            query = query.Where(m => m.EquipmentId == equipmentId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(m => m.ScheduledDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(m => m.ScheduledDate <= toDate.Value);
        }

        var list = await query.ToListAsync();
        return Ok(_mapper.Map<IEnumerable<MaintenanceResponseDto>>(list));
    }

    /// <summary>
    /// Get specific maintenance record by identifier.
    /// </summary>
    /// <param name="id">Unique identifier of the maintenance record.</param>
    /// <returns>Detailed maintenance record data.</returns>
    /// <response code="200">Record found and returned.</response>
    /// <response code="404">Maintenance record with specified Id not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MaintenanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MaintenanceResponseDto>> GetById(int id)
    {
        var maintenance = await _context.Maintenances.FindAsync(id);
        if (maintenance == null)
        {
            return NotFound($"Maintenance record with Id {id} not found.");
        }

        return Ok(_mapper.Map<MaintenanceResponseDto>(maintenance));
    }

    /// <summary>
    /// Update maintenance information or change its status.
    /// </summary>
    /// <remarks>
    /// Updates description, status, and performed date. Transitioning from Completed or Cancelled status is restricted.
    /// </remarks>
    /// <param name="id">Identifier of the maintenance record to update.</param>
    /// <param name="dto">New data for update.</param>
    /// <returns>Updated maintenance record.</returns>
    /// <response code="200">Record successfully updated.</response>
    /// <response code="400">Validation error or invalid status transition.</response>
    /// <response code="404">Maintenance record with specified Id not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(MaintenanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MaintenanceResponseDto>> Update(int id, [FromBody] UpdateMaintenanceDto dto)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        var maintenance = await _context.Maintenances.FindAsync(id);
        if (maintenance == null)
        {
            return NotFound($"Maintenance record with Id {id} not found.");
        }

        if (maintenance.Status == MaintenanceStatus.Completed || maintenance.Status == MaintenanceStatus.Cancelled)
        {
            return BadRequest($"Cannot change status for maintenance with status {maintenance.Status}.");
        }

        _mapper.Map(dto, maintenance);

        if (maintenance.Status == MaintenanceStatus.Completed && !maintenance.PerformedDate.HasValue)
        {
            maintenance.PerformedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var response = _mapper.Map<MaintenanceResponseDto>(maintenance);
        return Ok(response);
    }
}