using FileCategorization_Shared.Common;
using FileCategorization_Api.Contracts.Actions;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using FileCategorization_Shared.DTOs.FileManagement;
using FileCategorization_Api.Interfaces;
using Hangfire;

namespace FileCategorization_Api.Services;

/// <summary>
/// Service implementation for actions operations with modern async patterns.
/// Coordinates between repositories, background jobs, and ML services.
/// </summary>
public class ActionsService : IActionsService
{
    private readonly IActionsRepository _actionsRepository;
    private readonly IMachineLearningService _machineLearningService;
    private readonly IConfigsService _configsService;
    private readonly IHangFireJobService _hangFireJobService;
    private readonly ILogger<ActionsService> _logger;

    /// <summary>
    /// Initializes a new instance of the ActionsService class.
    /// </summary>
    public ActionsService(
        IActionsRepository actionsRepository,
        IMachineLearningService machineLearningService,
        IConfigsService configsService,
        IHangFireJobService hangFireJobService,
        ILogger<ActionsService> logger)
    {
        _actionsRepository = actionsRepository ?? throw new ArgumentNullException(nameof(actionsRepository));
        _machineLearningService = machineLearningService ?? throw new ArgumentNullException(nameof(machineLearningService));
        _configsService = configsService ?? throw new ArgumentNullException(nameof(configsService));
        _hangFireJobService = hangFireJobService ?? throw new ArgumentNullException(nameof(hangFireJobService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Result<ActionJobResponse>> RefreshFilesAsync(RefreshFilesRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting refresh files operation with batch size {BatchSize}", request.BatchSize);

            // Enqueue background job
            var jobId = BackgroundJob.Enqueue<IHangFireJobService>(job =>
                job.RefreshFiles(cancellationToken));

            var response = new ActionJobResponse
            {
                JobId = jobId,
                Status = "Queued",
                StartTime = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["BatchSize"] = request.BatchSize,
                    ["ForceRecategorization"] = request.ForceRecategorization,
                    ["FileExtensionFilters"] = request.FileExtensionFilters ?? new List<string>()
                }
            };

            _logger.LogInformation("Refresh files job queued with ID: {JobId}", jobId);

            return Result<ActionJobResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start refresh files operation");
            return Result<ActionJobResponse>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ActionJobResponse>> MoveFilesAsync(MoveFilesRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting move files operation for {Count} files", request.FilesToMove.Count);

            // Validate that files exist before queuing job
            var fileIds = request.FilesToMove.Select(f => f.Id).ToList();
            var existingFilesResult = await _actionsRepository.GetFilesByIdsAsync(fileIds, cancellationToken);

            if (existingFilesResult.IsFailure)
            {
                return Result<ActionJobResponse>.Failure($"Failed to validate files: {existingFilesResult.Error}");
            }

            var missingFiles = fileIds.Except(existingFilesResult.Value!.Keys).ToList();
            if (missingFiles.Any() && !request.ContinueOnError)
            {
                return Result<ActionJobResponse>.Failure($"Files not found: {string.Join(", ", missingFiles)}");
            }

            // Enqueue background job
            var jobId = BackgroundJob.Enqueue<IHangFireJobService>(job =>
                job.MoveFilesJob(request.FilesToMove, cancellationToken));

            var response = new ActionJobResponse
            {
                JobId = jobId,
                Status = "Queued",
                StartTime = DateTime.UtcNow,
                TotalItems = request.FilesToMove.Count,
                Metadata = new Dictionary<string, object>
                {
                    ["ContinueOnError"] = request.ContinueOnError,
                    ["ValidateCategories"] = request.ValidateCategories,
                    ["CreateDirectories"] = request.CreateDirectories,
                    ["MissingFiles"] = missingFiles
                }
            };

            _logger.LogInformation("Move files job queued with ID: {JobId}", jobId);

            return Result<ActionJobResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start move files operation");
            return Result<ActionJobResponse>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ActionJobResponse>> ForceCategorizeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting force categorization background job");

            // Enqueue background job for force categorization
            var jobId = BackgroundJob.Enqueue<IHangFireJobService>(job =>
                job.ForceCategorizeJob(cancellationToken));

            _logger.LogInformation("Force categorization job queued with ID: {JobId}", jobId);

            var response = new ActionJobResponse
            {
                JobId = jobId,
                Status = "Queued",
                StartTime = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["Operation"] = "ForceCategorize",
                    ["Description"] = "Force categorization of uncategorized files using ML prediction"
                }
            };

            return Result<ActionJobResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue force categorization job");
            return Result<ActionJobResponse>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<TrainModelResponse>> TrainModelAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting ML model training background job");

            // Enqueue background job for ML model training
            var jobId = BackgroundJob.Enqueue<IHangFireJobService>(job =>
                job.TrainModelJob(cancellationToken));

            _logger.LogInformation("ML model training job queued with ID: {JobId}", jobId);

            var response = new TrainModelResponse
            {
                Success = true,
                Message = "Model training job has been queued and will execute in background",
                JobId = jobId,
                ModelVersion = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                TrainingDuration = TimeSpan.Zero, // Will be updated when job completes
                Metrics = new Dictionary<string, double>
                {
                    ["JobQueued"] = 1
                }
            };

            return Result<TrainModelResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue ML model training job");
            return Result<TrainModelResponse>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ActionJobResponse>> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return Result<ActionJobResponse>.Failure("Job ID cannot be null or empty");
            }

            _logger.LogDebug("Getting status for job: {JobId}", jobId);

            // Get job status from Hangfire JobStorage
            var jobStorage = JobStorage.Current;
            if (jobStorage == null)
            {
                _logger.LogWarning("Hangfire JobStorage is not initialized");
                return Result<ActionJobResponse>.Failure("Current JobStorage instance has not been initialized");
            }

            using var connection = jobStorage.GetConnection();
            var jobData = connection.GetJobData(jobId);

            if (jobData == null)
            {
                _logger.LogWarning("Job {JobId} not found in Hangfire storage", jobId);
                return Result<ActionJobResponse>.Failure($"Job with ID '{jobId}' not found");
            }

            // Get job state information
            var stateData = connection.GetStateData(jobId);

            // Build response with real Hangfire data
            var response = new ActionJobResponse
            {
                JobId = jobId,
                Status = MapHangfireStateToStatus(stateData?.Name),
                StartTime = jobData.CreatedAt,
                Metadata = new Dictionary<string, object>
                {
                    ["JobType"] = jobData.Job?.Type?.Name ?? "Unknown",
                    ["JobMethod"] = jobData.Job?.Method?.Name ?? "Unknown",
                    ["StateReason"] = stateData?.Reason ?? "No reason provided",
                    ["StateData"] = stateData?.Data ?? new Dictionary<string, string>(),
                    ["Arguments"] = jobData.Job?.Args?.Select(arg => arg?.ToString() ?? "null").ToArray() ?? Array.Empty<string>()
                }
            };

            // Add progress information if available from state data
            if (stateData?.Data != null)
            {
                // Look for progress indicators in state data
                if (stateData.Data.TryGetValue("Progress", out var progressValue) && 
                    int.TryParse(progressValue, out var progress))
                {
                    response.ProcessedItems = progress;
                    response.TotalItems = 100; // Assuming percentage-based progress
                }

                // Look for processed items count
                if (stateData.Data.TryGetValue("ProcessedItems", out var processedValue) &&
                    int.TryParse(processedValue, out var processed))
                {
                    response.ProcessedItems = processed;
                }

                // Look for total items count
                if (stateData.Data.TryGetValue("TotalItems", out var totalValue) &&
                    int.TryParse(totalValue, out var total))
                {
                    response.TotalItems = total;
                }

                // Add completion time if available
                if (stateData.Data.TryGetValue("CompletedAt", out var completedAtValue) &&
                    DateTime.TryParse(completedAtValue, out var completedAt))
                {
                    response.EndTime = completedAt;
                    response.Metadata["CompletedAt"] = completedAt.ToString("O");
                }
            }

            // Calculate duration if job is completed or failed
            if (response.EndTime.HasValue)
            {
                response.Metadata["Duration"] = (response.EndTime.Value - response.StartTime).ToString();
            }
            else if (response.Status is "Processing" or "Running")
            {
                response.Metadata["RunningDuration"] = (DateTime.UtcNow - response.StartTime).ToString();
            }

            _logger.LogInformation("Retrieved status for job {JobId}: {Status}", jobId, response.Status);
            return Result<ActionJobResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job status for {JobId}", jobId);
            return Result<ActionJobResponse>.FromException(ex);
        }
    }

    /// <summary>
    /// Maps Hangfire job states to our standardized status strings.
    /// </summary>
    private static string MapHangfireStateToStatus(string? hangfireState)
    {
        return hangfireState?.ToLowerInvariant() switch
        {
            "enqueued" => "Queued",
            "processing" => "Running",
            "succeeded" => "Completed",
            "failed" => "Failed",
            "deleted" => "Cancelled",
            "scheduled" => "Scheduled",
            "awaiting" => "Waiting",
            null => "Unknown",
            _ => hangfireState
        };
    }
}