using FileCategorization_Api.Common;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using FileCategorization_Api.Domain.Entities.FilesDetail;
using FileCategorization_Api.Infrastructure.Data;
using FileCategorization_Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FileCategorization_Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for actions-related database operations.
/// Provides optimized batch operations following Repository Pattern with Result Pattern.
/// </summary>
public class ActionsRepository : IActionsRepository
{
    private readonly ApplicationContext _context;
    private readonly ILogger<ActionsRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the ActionsRepository class.
    /// </summary>
    /// <param name="context">Database context for Entity Framework operations</param>
    /// <param name="logger">Logger for structured logging</param>
    public ActionsRepository(ApplicationContext context, ILogger<ActionsRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Result<Dictionary<int, FilesDetail>>> GetFilesByIdsAsync(List<int> fileIds, CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileIds == null || !fileIds.Any())
            {
                return Result<Dictionary<int, FilesDetail>>.Success(new Dictionary<int, FilesDetail>());
            }

            _logger.LogDebug("Getting {Count} files by IDs", fileIds.Count);

            var files = await _context.FilesDetail
                .Where(f => fileIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, cancellationToken);

            _logger.LogInformation("Retrieved {FoundCount}/{RequestedCount} files from database", 
                files.Count, fileIds.Count);

            return Result<Dictionary<int, FilesDetail>>.Success(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get files by IDs: {FileIds}", string.Join(",", fileIds));
            return Result<Dictionary<int, FilesDetail>>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<int>> BatchUpdateFilesAsync(List<FilesDetail> files, CancellationToken cancellationToken = default)
    {
        try
        {
            if (files == null || !files.Any())
            {
                return Result<int>.Success(0);
            }

            _logger.LogDebug("Batch updating {Count} files", files.Count);

            // Set common update properties
            var updateTime = DateTime.UtcNow;
            foreach (var file in files)
            {
                file.LastUpdatedDate = updateTime;
            }

            _context.FilesDetail.UpdateRange(files);
            var updatedCount = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully batch updated {Count} files", updatedCount);

            return Result<int>.Success(updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch update {Count} files", files?.Count ?? 0);
            return Result<int>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<HashSet<string>>> GetExistingFileNamesAsync(List<string> fileNames, CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileNames == null || !fileNames.Any())
            {
                return Result<HashSet<string>>.Success(new HashSet<string>());
            }

            _logger.LogDebug("Checking existence of {Count} file names", fileNames.Count);

            var existingNames = await _context.FilesDetail
                .Where(f => fileNames.Contains(f.Name))
                .Select(f => f.Name)
                .ToListAsync(cancellationToken);

            var resultSet = existingNames.ToHashSet();

            _logger.LogInformation("Found {ExistingCount}/{TotalCount} existing files", 
                resultSet.Count, fileNames.Count);

            return Result<HashSet<string>>.Success(resultSet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existing file names for {Count} files", fileNames?.Count ?? 0);
            return Result<HashSet<string>>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<int>> BatchAddFilesAsync(List<FilesDetail> files, CancellationToken cancellationToken = default)
    {
        try
        {
            if (files == null || !files.Any())
            {
                return Result<int>.Success(0);
            }

            _logger.LogDebug("Batch adding {Count} files", files.Count);

            // Set common properties for new files
            var currentTime = DateTime.UtcNow;
            foreach (var file in files)
            {
                file.CreatedDate = currentTime;
                file.LastUpdatedDate = currentTime;
                file.IsActive = true;
                file.IsDeleted = false;
            }

            await _context.FilesDetail.AddRangeAsync(files, cancellationToken);
            var addedCount = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully batch added {Count} files", addedCount);

            return Result<int>.Success(addedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch add {Count} files", files?.Count ?? 0);
            return Result<int>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<int>> BatchAppendTrainingDataAsync(List<string> trainingEntries, string trainingFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (trainingEntries == null || !trainingEntries.Any())
            {
                return Result<int>.Success(0);
            }

            if (string.IsNullOrWhiteSpace(trainingFilePath))
            {
                return Result<int>.Failure("Training file path cannot be null or empty");
            }

            _logger.LogDebug("Batch appending {Count} training data entries to {FilePath}", 
                trainingEntries.Count, trainingFilePath);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(trainingFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created training data directory: {Directory}", directory);
            }

            await File.AppendAllLinesAsync(trainingFilePath, trainingEntries, cancellationToken);

            _logger.LogInformation("Successfully appended {Count} training data entries", trainingEntries.Count);

            return Result<int>.Success(trainingEntries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append {Count} training data entries to {FilePath}", 
                trainingEntries?.Count ?? 0, trainingFilePath);
            return Result<int>.FromException(ex);
        }
    }
}