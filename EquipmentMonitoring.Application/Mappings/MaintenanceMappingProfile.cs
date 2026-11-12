using AutoMapper;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.Mappings;

public class MaintenanceMappingProfile : Profile
{
    public MaintenanceMappingProfile()
    {
        CreateMap<Maintenance, MaintenanceResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<CreateMaintenanceDto, Maintenance>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => MaintenanceStatus.Scheduled));

        CreateMap<UpdateMaintenanceDto, Maintenance>();
    }
}