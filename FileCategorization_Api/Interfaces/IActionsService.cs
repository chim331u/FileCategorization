using FileCategorization_Shared.Common;
using FileCategorization_Api.Contracts.Actions;

namespace FileCategorization_Api.Interfaces;

/// <summary>
/// Service interface for actions operations with modern async patterns.
/// Provides high-level business logic for file management actions.
/// </summary>
public interface IActionsService
{
    /// <summary>
    /// Initiates a background job to refresh files from the origin directory.
    /// Scans for new files, categorizes them using ML, and adds them to the database.
    /// </summary>
    /// <param name="request">Refresh files configuration</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Job information with tracking details</returns>
    Task<Result<ActionJobResponse>> RefreshFilesAsync(RefreshFilesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a background job to move files to their categorized directories.
    /// Updates database records and appends training data efficiently.
    /// </summary>
    /// <param name="request">Move files configuration with target categories</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Job information with tracking details</returns>
    Task<Result<ActionJobResponse>> MoveFilesAsync(MoveFilesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces re-categorization of all files marked for categorization.
    /// Uses the current ML model to predict categories for uncategorized files.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result with categorization summary</returns>
    Task<Result<ActionJobResponse>> ForceCategorizeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Trains and saves a new machine learning model using current training data.
    /// Returns detailed information about the training process and model metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Training result with model information</returns>
    Task<Result<TrainModelResponse>> TrainModelAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status and progress of a background job.
    /// </summary>
    /// <param name="jobId">Unique job identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Current job status and progress information</returns>
    Task<Result<ActionJobResponse>> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default);
}