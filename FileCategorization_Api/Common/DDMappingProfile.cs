using AutoMapper;
using FileCategorization_Api.Contracts.DD;
using FileCategorization_Api.Domain.Entities.DD;
using FileCategorization_Api.Domain.Entities.DD_Web;

namespace FileCategorization_Api.Common;

/// <summary>
/// AutoMapper profile for DD-related mappings
/// </summary>
public class DDMappingProfile : Profile
{
    public DDMappingProfile()
    {
        // Thread mappings
        CreateMap<DD_Threads, ThreadSummaryDto>()
            .ForMember(dest => dest.LinksCount, opt => opt.Ignore())
            .ForMember(dest => dest.NewLinksCount, opt => opt.Ignore())
            .ForMember(dest => dest.UsedLinksCount, opt => opt.Ignore())
            .ForMember(dest => dest.HasNewLinks, opt => opt.Ignore());

        // Link mappings
        CreateMap<DD_LinkEd2k, LinkDto>()
            .ForMember(dest => dest.ThreadId, opt => opt.MapFrom(src => src.Threads.Id));

        CreateMap<DD_LinkEd2k, LinkUsageResultDto>()
            .ForMember(dest => dest.LinkId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ThreadId, opt => opt.MapFrom(src => src.Threads.Id))
            .ForMember(dest => dest.UsedAt, opt => opt.MapFrom(src => src.LastUpdatedDate != default(DateTime) ? src.LastUpdatedDate : DateTime.Now));

        // Legacy DTO mappings (for backward compatibility)
        CreateMap<DD_Threads, ThreadsDto>()
            .ForMember(dest => dest.LinksNumber, opt => opt.Ignore())
            .ForMember(dest => dest.NewLinks, opt => opt.Ignore());

        CreateMap<DD_LinkEd2k, Ed2kLinkDto>()
            .ForMember(dest => dest.ThreadId, opt => opt.MapFrom(src => src.Threads.Id));
    }
}