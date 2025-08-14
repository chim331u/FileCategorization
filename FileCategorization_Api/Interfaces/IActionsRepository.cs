using FileCategorization_Api.Common;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using FileCategorization_Api.Domain.Entities.FilesDetail;

namespace FileCategorization_Api.Interfaces;

/// <summary>
/// Repository interface for actions-related database operations.
/// Follows Repository Pattern with Result Pattern for structured error handling.
/// </summary>
public interface IActionsRepository
{
    /// <summary>
    /// Gets multiple files by their IDs in a single query to avoid N+1 problem.
    /// </summary>
    /// <param name="fileIds">List of file IDs to retrieve</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Dictionary mapping file ID to FilesDetail entity</returns>
    Task<Result<Dictionary<int, FilesDetail>>> GetFilesByIdsAsync(List<int> fileIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch updates multiple file records in a single transaction.
    /// </summary>
    /// <param name="files">List of files to update</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Number of records updated</returns>
    Task<Result<int>> BatchUpdateFilesAsync(List<FilesDetail> files, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets existing file names from a list to check duplicates efficiently.
    /// </summary>
    /// <param name="fileNames">List of file names to check</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>HashSet of existing file names</returns>
    Task<Result<HashSet<string>>> GetExistingFileNamesAsync(List<string> fileNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch adds new file records with automatic property setting.
    /// </summary>
    /// <param name="files">List of files to add</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Number of records added</returns>
    Task<Result<int>> BatchAddFilesAsync(List<FilesDetail> files, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends training data entries to the training file in batch.
    /// </summary>
    /// <param name="trainingEntries">List of training data entries</param>
    /// <param name="trainingFilePath">Full path to training data file</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Number of entries written</returns>
    Task<Result<int>> BatchAppendTrainingDataAsync(List<string> trainingEntries, string trainingFilePath, CancellationToken cancellationToken = default);
}