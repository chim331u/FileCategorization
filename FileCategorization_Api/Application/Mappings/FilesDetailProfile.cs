using AutoMapper;
using FileCategorization_Api.Contracts.FilesDetail;
using FileCategorization_Api.Models.FileCategorization;

namespace FileCategorization_Api.Application.Mappings;

/// <summary>
/// AutoMapper profile for FilesDetail entity mappings.
/// </summary>
public class FilesDetailProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the FilesDetailProfile class.
    /// </summary>
    public FilesDetailProfile()
    {
        // Entity to Response DTO mapping
        CreateMap<FilesDetail, FilesDetailResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Path, opt => opt.MapFrom(src => src.Path))
            .ForMember(dest => dest.FileCategory, opt => opt.MapFrom(src => src.FileCategory))
            .ForMember(dest => dest.IsToCategorize, opt => opt.MapFrom(src => src.IsToCategorize))
            .ForMember(dest => dest.IsNew, opt => opt.MapFrom(src => src.IsNew))
            .ForMember(dest => dest.FileSize, opt => opt.MapFrom(src => src.FileSize))
            .ForMember(dest => dest.IsNotToMove, opt => opt.MapFrom(src => src.IsNotToMove));

        // Request DTO to Entity mapping
        CreateMap<FilesDetailRequest, FilesDetail>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()) // ID should not be set from request
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Path, opt => opt.MapFrom(src => src.Path))
            .ForMember(dest => dest.FileCategory, opt => opt.MapFrom(src => src.FileCategory))
            .ForMember(dest => dest.IsToCategorize, opt => opt.MapFrom(src => true)) // Default to requiring categorization
            .ForMember(dest => dest.IsNew, opt => opt.MapFrom(src => true)) // New files are marked as new
            .ForMember(dest => dest.FileSize, opt => opt.MapFrom(src => src.FileSize))
            .ForMember(dest => dest.IsNotToMove, opt => opt.MapFrom(src => false)) // Default to movable
            .ForMember(dest => dest.LastUpdateFile, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.Note, opt => opt.Ignore());

        // Update Request DTO to Entity mapping
        CreateMap<FilesDetailUpdateRequest, FilesDetail>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()) // ID should not be updated
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Path, opt => opt.MapFrom(src => src.Path))
            .ForMember(dest => dest.FileCategory, opt => opt.MapFrom(src => src.FileCategory))
            .ForMember(dest => dest.IsToCategorize, opt => opt.MapFrom(src => src.IsToCategorize))
            .ForMember(dest => dest.IsNew, opt => opt.MapFrom(src => src.IsNew))
            .ForMember(dest => dest.FileSize, opt => opt.MapFrom(src => src.FileSize))
            .ForMember(dest => dest.IsNotToMove, opt => opt.MapFrom(src => src.IsNotToMove))
            .ForMember(dest => dest.LastUpdateFile, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore()) // Don't update creation date
            .ForMember(dest => dest.IsActive, opt => opt.Ignore()) // Don't change active status
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore()) // Don't change deleted status
            .ForMember(dest => dest.Note, opt => opt.Ignore());

        // FileMoveDto mappings (minimal mapping for move operations)
        CreateMap<FileMoveDto, FilesDetail>()
            .ForMember(dest => dest.FileCategory, opt => opt.MapFrom(src => src.FileCategory))
            .ForMember(dest => dest.LastUpdatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsToCategorize, opt => opt.MapFrom(src => false)) // Mark as categorized when moved
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null)); // Only map non-null members
    }
}