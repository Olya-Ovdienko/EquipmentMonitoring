using AutoMapper;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.API.Controllers;

/// <summary>
/// Контролер для управління планами та обліком технічного обслуговування обладнання.
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
    /// Створити новий запис про технічне обслуговування.
    /// </summary>
    /// <remarks>
    /// Створює новий план обслуговування для існуючого обладнання. Дата ScheduledDate повинна бути в майбутньому.
    /// </remarks>
    /// <param name="dto">Дані для створення технічного обслуговування.</param>
    /// <returns>Створений запис технічного обслуговування.</returns>
    /// <response code="201">Запис успішно створено.</response>
    /// <response code="400">Помилка валідації вхідних даних (наприклад, дата з минулого або порожній опис).</response>
    /// <response code="404">Обладнання з вказаним EquipmentId не знайдено.</response>
    /// <response code="500">Внутрішня помилка сервера.</response>
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

        var equipment = await _context.Equipments.FirstOrDefaultAsync();
        if (equipment == null || dto.EquipmentId <= 0)
        {
            return NotFound($"Обладнання з Id {dto.EquipmentId} не знайдено.");
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
    /// <remarks>
    /// Повертає список всіх записів про технічне обслуговування. Доступна фільтрація за обладнанням, статусом та діапазоном дат.
    /// </remarks>
    /// <param name="equipmentId">Опціональний фільтр за ідентифікатором обладнання.</param>
    /// <param name="status">Опціональний фільтр за статусом (Scheduled, InProgress, Completed, Cancelled).</param>
    /// <param name="fromDate">Початкова дата діапазону для фільтрації scheduledDate.</param>
    /// <param name="toDate">Кінцева дата діапазону для фільтрації scheduledDate.</param>
    /// <returns>Список відповідних записів про обслуговування.</returns>
    /// <response code="200">Успішне отримання списку.</response>
    /// <response code="500">Внутрішня помилка сервера.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MaintenanceResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<MaintenanceResponseDto>>> GetAll(
        [FromQuery] int? equipmentId,
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
    /// <param name="id">Унікальний ідентифікатор запису обслуговування.</param>
    /// <returns>Детальні дані запису про обслуговування.</returns>
    /// <response code="200">Запис знайдено та повернуто.</response>
    /// <response code="404">Запис про обслуговування з вказаним Id не знайдено.</response>
    /// <response code="500">Внутрішня помилка сервера.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MaintenanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
    /// <remarks>
    /// Дозволяє оновити опис, статус та дату виконання. Перехід зі статусів Completed або Cancelled заборонено.
    /// </remarks>
    /// <param name="id">Ідентифікатор запису обслуговування для оновлення.</param>
    /// <param name="dto">Нові дані для оновлення.</param>
    /// <returns>Оновлений запис обслуговування.</returns>
    /// <response code="200">Запис успішно оновлено.</response>
    /// <response code="400">Помилка валідації або спроба некоректного переходу статусу.</response>
    /// <response code="404">Запис про обслуговування з вказаним Id не знайдено.</response>
    /// <response code="500">Внутрішня помилка сервера.</response>
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