using AutoMapper;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.API.Controllers;

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
    /// Створити новий запис про технічне обслуговування.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MaintenanceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceResponseDto>> Create([FromBody] CreateMaintenanceDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        // Шукаємо обладнання у базі
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
    /// Отримати список записів про обслуговування з фільтрацією.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MaintenanceResponseDto>), StatusCodes.Status200OK)]
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
    /// Отримати конкретний запис про обслуговування за ідентифікатором.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MaintenanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceResponseDto>> GetById(int id)
    {
        var maintenance = await _context.Maintenances.FindAsync(id);
        if (maintenance == null)
        {
            return NotFound($"Запис про обслуговування з Id {id} не знайдено.");
        }

        return Ok(_mapper.Map<MaintenanceResponseDto>(maintenance));
    }

    /// <summary>
    /// Оновити інформацію про обслуговування або змінити його статус.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(MaintenanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
            return NotFound($"Запис про обслуговування з Id {id} не знайдено.");
        }

        if (maintenance.Status == MaintenanceStatus.Completed || maintenance.Status == MaintenanceStatus.Cancelled)
        {
            return BadRequest($"Неможливо змінити статус для обслуговування зі статусом {maintenance.Status}.");
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