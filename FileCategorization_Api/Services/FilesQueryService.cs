using FileCategorization_Api.Domain.Enums;
using FileCategorization_Api.Domain.Entities.FilesDetail;
using AutoMapper;
using FileCategorization_Shared.DTOs.FileManagement;
using FileCategorization_Shared.Common;
using FileCategorization_Shared.Enums;
using FileCategorization_Api.Interfaces;

namespace FileCategorization_Api.Services;

/// <summary>
/// Service for handling file queries and retrieval operations.
/// </summary>
public class FilesQueryService : IFilesQueryService
{
    private readonly IFilesDetailRepository _filesRepository;
    private readonly ILogger<FilesQueryService> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the FilesQueryService class.
    /// </summary>
    /// <param name="filesRepository">The files repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public FilesQueryService(IFilesDetailRepository filesRepository, ILogger<FilesQueryService> logger, IMapper mapper)
    {
        _filesRepository = filesRepository;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets all distinct file categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the list of categories.</returns>
    public async Task<Result<IEnumerable<string>>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving file categories");
        return await _filesRepository.GetCategoriesAsync(cancellationToken);
    }

    /// <summary>
    /// Gets files filtered by the specified criteria.
    /// </summary>
    /// <param name="filterType">The filter type to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the filtered files.</returns>
    public async Task<Result<IEnumerable<FilesDetailResponse>>> GetFilteredFilesAsync(FileFilterType filterType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving files with filter: {FilterType}", filterType);

        var filesResult = await _filesRepository.GetFilteredFilesAsync((int)filterType, cancellationToken);
        
        if (filesResult.IsFailure)
            return Result<IEnumerable<FilesDetailResponse>>.Failure(filesResult.Error!);

        // Map to DTOs using AutoMapper
        var responseList = _mapper.Map<IEnumerable<FilesDetailResponse>>(filesResult.Value);

        return Result<IEnumerable<FilesDetailResponse>>.Success(responseList);
    }

    /// <summary>
    /// Gets the latest files for each category.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the latest files by category.</returns>
    public async Task<Result<IEnumerable<FilesDetailResponse>>> GetLastViewListAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving latest files by category for last view");

        var filesResult = await _filesRepository.GetLatestFilesByCategoryAsync(cancellationToken);
        
        if (filesResult.IsFailure)
            return Result<IEnumerable<FilesDetailResponse>>.Failure(filesResult.Error!);

        // Map to DTOs using AutoMapper
        var responseList = _mapper.Map<IEnumerable<FilesDetailResponse>>(filesResult.Value);

        return Result<IEnumerable<FilesDetailResponse>>.Success(responseList);
    }

    /// <summary>
    /// Gets files that need to be categorized.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing files to categorize.</returns>
    public async Task<Result<IEnumerable<FilesDetailResponse>>> GetFilesToCategorizeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving files that need categorization");

        var filesResult = await _filesRepository.GetToCategorizeAsync(cancellationToken);
        
        if (filesResult.IsFailure)
            return Result<IEnumerable<FilesDetailResponse>>.Failure(filesResult.Error!);

        // Map to DTOs using AutoMapper
        var responseList = _mapper.Map<IEnumerable<FilesDetailResponse>>(filesResult.Value);

        return Result<IEnumerable<FilesDetailResponse>>.Success(responseList);
    }

    /// <summary>
    /// Searches files by name pattern.
    /// </summary>
    /// <param name="namePattern">The pattern to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing matching files.</returns>
    public async Task<Result<IEnumerable<FilesDetailResponse>>> SearchFilesByNameAsync(string namePattern, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(namePattern))
            return Result<IEnumerable<FilesDetailResponse>>.Failure("Search pattern cannot be empty");

        _logger.LogInformation("Searching files by name pattern: {Pattern}", namePattern);

        var filesResult = await _filesRepository.SearchByNameAsync(namePattern, cancellationToken);
        
        if (filesResult.IsFailure)
            return Result<IEnumerable<FilesDetailResponse>>.Failure(filesResult.Error!);

        // Map to DTOs using AutoMapper
        var responseList = _mapper.Map<IEnumerable<FilesDetailResponse>>(filesResult.Value);

        return Result<IEnumerable<FilesDetailResponse>>.Success(responseList);
    }

    /// <summary>
    /// Gets files by category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the files in the specified category.</returns>
    public async Task<Result<IEnumerable<FilesDetailResponse>>> GetFilesByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return Result<IEnumerable<FilesDetailResponse>>.Failure("Category cannot be null or empty");
        }

        _logger.LogInformation("Retrieving files for category: {Category}", category);

        var filesResult = await _filesRepository.GetByCategoryAsync(category, cancellationToken);

        if (filesResult.IsFailure)
        {
            _logger.LogError("Failed to retrieve files by category: {Error}", filesResult.Error);
            return Result<IEnumerable<FilesDetailResponse>>.Failure(filesResult.Error!);
        }

        // Map to DTOs using AutoMapper
        var responseList = _mapper.Map<IEnumerable<FilesDetailResponse>>(filesResult.Value);

        _logger.LogInformation("Retrieved {Count} files for category: {Category}", responseList.Count(), category);
        return Result<IEnumerable<FilesDetailResponse>>.Success(responseList);
    }
}

/// <summary>
/// Interface for file query operations.
/// </summary>
public interface IFilesQueryService
{
    /// <summary>
    /// Gets all distinct file categories.
    /// </summary>
    Task<Result<IEnumerable<string>>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets files filtered by the specified criteria.
    /// </summary>
    Task<Result<IEnumerable<FilesDetailResponse>>> GetFilteredFilesAsync(FileFilterType filterType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest files for each category.
    /// </summary>
    Task<Result<IEnumerable<FilesDetailResponse>>> GetLastViewListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets files that need to be categorized.
    /// </summary>
    Task<Result<IEnumerable<FilesDetailResponse>>> GetFilesToCategorizeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches files by name pattern.
    /// </summary>
    Task<Result<IEnumerable<FilesDetailResponse>>> SearchFilesByNameAsync(string namePattern, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets files by category.
    /// </summary>
    Task<Result<IEnumerable<FilesDetailResponse>>> GetFilesByCategoryAsync(string category, CancellationToken cancellationToken = default);
}