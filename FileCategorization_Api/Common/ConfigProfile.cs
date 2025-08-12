using AutoMapper;
using FileCategorization_Api.Domain.Entities.Config;
using FileCategorization_Api.Domain.Entities.FileCategorization;

namespace FileCategorization_Api.Common;

/// <summary>
/// AutoMapper profile for configuration entity mappings.
/// </summary>
public class ConfigProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the ConfigProfile class.
    /// </summary>
    public ConfigProfile()
    {
        // Entity to Response DTO mapping
        CreateMap<Configs, ConfigResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Key, opt => opt.MapFrom(src => src.Key ?? string.Empty))
            .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Value ?? string.Empty))
            // .ForMember(dest => dest.IsDev, opt => opt.MapFrom(src => src.IsDev))
            // .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
            // .ForMember(dest => dest.LastUpdatedDate, opt => opt.MapFrom(src => src.LastUpdatedDate))
            // .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            ;

        // Request DTO to Entity mapping
        CreateMap<ConfigRequest, Configs>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()) // ID should not be set from request
            .ForMember(dest => dest.Key, opt => opt.MapFrom(src => src.Key))
            .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Value))
            .ForMember(dest => dest.IsDev, opt => opt.MapFrom(src => src.IsDev))
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

        // Update Request DTO to Entity mapping (partial update)
        CreateMap<ConfigUpdateRequest, Configs>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()) // ID should not be updated
            .ForMember(dest => dest.Key, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Key)))
            .ForMember(dest => dest.Value, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Value)))
            .ForMember(dest => dest.IsDev, opt => opt.Condition(src => src.IsDev.HasValue))
            .ForMember(dest => dest.IsDev, opt => opt.MapFrom(src => src.IsDev!.Value))
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore()) // Don't update creation date
            .ForMember(dest => dest.IsActive, opt => opt.Ignore()) // Don't change active status
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}