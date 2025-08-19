using FileCategorization_Api.Infrastructure.Data;
using FileCategorization_Shared.Common;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using Microsoft.EntityFrameworkCore;

namespace FileCategorization_Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for FilesDetail entity operations.
/// </summary>
public class FilesDetailRepository : Repository<FilesDetail>, IFilesDetailRepository
{
    /// <summary>
    /// Initializes a new instance of the FilesDetailRepository class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public FilesDetailRepository(ApplicationContext context, ILogger<Repository<FilesDetail>> logger) 
        : base(context, logger)
    {
    }

    /// <summary>
    /// Gets files by their category.
    /// </summary>
    public async Task<Result<IEnumerable<FilesDetail>>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
                return Result<IEnumerable<FilesDetail>>.Failure("Category cannot be null or empty");

            var files = await _dbSet
                .AsNoTracking()
                .Where(f => f.IsActive && f.FileCategory == category && !f.IsNotToMove)
                .OrderBy(f => f.Name)
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<FilesDetail>>.Success(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving files by category: {Category}", category);
            return Result<IEnumerable<FilesDetail>>.FromException(ex);
        }
    }

    /// <summary>
    /// Gets all files that need to be categorized.
    /// </summary>
    public async Task<Result<IEnumerable<FilesDetail>>> GetToCategorizeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var files = await _dbSet
                .AsNoTracking()
                .Where(f => f.IsActive && f.IsToCategorize)
                .OrderBy(f => f.CreatedDate)
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<FilesDetail>>.Success(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving files to categorize");
            return Result<IEnumerable<FilesDetail>>.FromException(ex);
        }
    }

    /// <summary>
    /// Gets all distinct file categories.
    /// </summary>
    public async Task<Result<IEnumerable<string>>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await _dbSet
                .AsNoTracking()
                .Where(f => f.IsActive && !string.IsNullOrEmpty(f.FileCategory))
                .Select(f => f.FileCategory!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<string>>.Success(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file categories");
            return Result<IEnumerable<string>>.FromException(ex);
        }
    }

    /// <summary>
    /// Gets files that are marked as new.
    /// </summary>
    public async Task<Result<IEnumerable<FilesDetail>>> GetNewFilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var files = await _dbSet
                .AsNoTracking()
                .Where(f => f.IsActive && f.IsNew)
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<FilesDetail>>.Success(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving new files");
            return Result<IEnumerable<FilesDetail>>.FromException(ex);
        }
    }

    /// <summary>
    /// Gets files filtered by various criteria.
    /// </summary>
    public async Task<Result<IEnumerable<FilesDetail>>> GetFilteredFilesAsync(int filterType, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbSet.AsNoTracking().Where(f => f.IsActive);

            query = filterType switch
            {
                1 => query, // All files
                2 => query.Where(f => !f.IsToCategorize && !string.IsNullOrEmpty(f.FileCategory)), // Categorized
                3 => query.Where(f => f.IsToCategorize), // To categorize
                4 => query.Where(f => f.IsNew), // New files
                _ => query
            };

            var files = await query
                .OrderBy(f => f.Name)
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<FilesDetail>>.Success(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filtered files with filter type: {FilterType}", filterType);
            return Result<IEnumerable<FilesDetail>>.FromException(ex);
        }
    }

    /// <summary>
    /// Gets files that should not be moved.
    /// </summary>
    public async Task<Result<IEnumerable<FilesDetail>>> GetNotToMoveFilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var files = await _dbSet
                .AsNoTracking()
                .Where(f => f.IsActive && f.IsNotToMove)
                .OrderBy(f => f.Name)
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<FilesDetail>>.Success(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving files that should not be moved");
            return Result<IEnumerable<FilesDetail>>.FromException(ex);
        }
    }

    /// <summary>
    /// Gets the latest file for each category (used in GetLastView functionality).
    /// </summary>
    public async Task<Result<IEnumerable<FilesDetail>>> GetLatestFilesByCategoryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Load all eligible files into memory first, then group and process
            var allFiles = await _dbSet
                .AsNoTracking()
                .Where(f => f.IsActive && !f.IsNotToMove && f.FileCategory != null)
                .ToListAsync(cancellationToken);

            // Group by category and get the latest file for each category (in memory)
            var latestFiles = allFiles
                .GroupBy(f => f.FileCategory)
                .Select(g => g.OrderByDescending(f => f.Name).First())
                .OrderBy(f => f.FileCategory)
                .ToList();

            return Result<IEnumerable<FilesDetail>>.Success(latestFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest files by category");
            return Result<IEnumerable<FilesDetail>>.FromException(ex);
        }
    }

    /// <summary>
    /// Updates the categorization status of a file.
    /// </summary>
    public async Task<Result<bool>> UpdateCategorizationAsync(int fileId, string category, bool isToCategorize, CancellationToken cancellationToken = default)
    {
        try
        {
            var file = await _dbSet.FindAsync(new object[] { fileId }, cancellationToken);
            if (file == null)
                return Result<bool>.Failure($"File with ID {fileId} not found");

            file.FileCategory = category;
            file.IsToCategorize = isToCategorize;
            file.LastUpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated categorization for file {FileId}: Category={Category}, ToCategorize={ToCategorize}", 
                fileId, category, isToCategorize);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating categorization for file {FileId}", fileId);
            return Result<bool>.FromException(ex);
        }
    }

    /// <summary>
    /// Searches files by name pattern.
    /// </summary>
    public async Task<Result<IEnumerable<FilesDetail>>> SearchByNameAsync(string namePattern, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(namePattern))
                return Result<IEnumerable<FilesDetail>>.Failure("Search pattern cannot be null or empty");

            var files = await _dbSet
                .AsNoTracking()
                .Where(f => f.IsActive && f.Name != null && f.Name.Contains(namePattern))
                .OrderBy(f => f.Name)
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<FilesDetail>>.Success(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching files by name pattern: {Pattern}", namePattern);
            return Result<IEnumerable<FilesDetail>>.FromException(ex);
        }
    }

    /// <summary>
    /// Updates a file's LastUpdate to current time and sets IsNotToMove to false.
    /// </summary>
    public async Task<Result<bool>> UpdateNotShowAgainAsync(int fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate ID parameter
            if (fileId <= 0)
            {
                return Result<bool>.Failure($"Invalid file ID: {fileId}. ID must be a positive integer.");
            }

            var file = await _dbSet.FindAsync(new object[] { fileId }, cancellationToken);
            if (file == null)
            {
                _logger.LogWarning("File with ID {FileId} not found for NotShowAgain operation", fileId);
                return Result<bool>.Success(false);
            }

            // Update the file properties as specified
            file.IsNotToMove = false;
            file.LastUpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated file {FileId} for NotShowAgain: LastUpdateFile={LastUpdateFile}, IsNotToMove={IsNotToMove}", 
                fileId, file.LastUpdateFile, file.IsNotToMove);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file {FileId} for NotShowAgain operation", fileId);
            return Result<bool>.FromException(ex);
        }
    }
}