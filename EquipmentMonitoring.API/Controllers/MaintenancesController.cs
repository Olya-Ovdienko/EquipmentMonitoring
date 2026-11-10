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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MaintenanceResponseDto>>> GetAll(
        [FromQuery] Guid? equipmentId = null,
        [FromQuery] MaintenanceStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Maintenances
            .Include(m => m.Equipment)
            .AsNoTracking();

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

        var list = await query.ToListAsync(cancellationToken);
        return Ok(_mapper.Map<IEnumerable<MaintenanceResponseDto>>(list));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MaintenanceResponseDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var item = await _context.Maintenances
            .Include(m => m.Equipment)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(_mapper.Map<MaintenanceResponseDto>(item));
    }

    [HttpGet("equipment/{equipmentId:guid}")]
    public async Task<ActionResult<IEnumerable<MaintenanceResponseDto>>> GetByEquipmentId(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        var list = await _context.Maintenances
            .Include(m => m.Equipment)
            .Where(m => m.EquipmentId == equipmentId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return Ok(_mapper.Map<IEnumerable<MaintenanceResponseDto>>(list));
    }

    [HttpPost]
    public async Task<ActionResult<MaintenanceResponseDto>> Create([FromBody] CreateMaintenanceDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var equipment = await _context.Equipments.FindAsync(new object[] { dto.EquipmentId }, cancellationToken);
        if (equipment == null)
            return BadRequest($"Equipment with ID {dto.EquipmentId} does not exist.");

        var entity = _mapper.Map<Maintenance>(dto);
        entity.Equipment = equipment;

        _context.Maintenances.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<MaintenanceResponseDto>(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMaintenanceDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var entity = await _context.Maintenances.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
            return NotFound();

        _mapper.Map(dto, entity);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}