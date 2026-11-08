using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.Mappings;

public static class EquipmentMapper
{
    public static Equipment ToDomain(CreateEquipmentDto dto)
    {
        return new Equipment(dto.Name, dto.Type, dto.SerialNumber, dto.Location);
    }

    public static EquipmentResponseDto ToResponse(Equipment equipment)
    {
        return new EquipmentResponseDto
        {
            Id = equipment.Id,
            Name = equipment.Name,
            Type = equipment.Type,
            SerialNumber = equipment.SerialNumber,
            Location = equipment.Location,
            Status = equipment.Status.ToString(),
            CreatedAt = equipment.CreatedAt
        };
    }
}
