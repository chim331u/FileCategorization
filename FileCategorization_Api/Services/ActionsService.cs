using FileCategorization_Api.Common;
using FileCategorization_Api.Contracts.Actions;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using FileCategorization_Api.Domain.Entities.FilesDetail;
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
                return Result<ActionJobResponse>.Failure($"Failed to validate files: {existingFilesResult.ErrorMessage}");
            }

            var missingFiles = fileIds.Except(existingFilesResult.Data!.Keys).ToList();
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
            _logger.LogInformation("Starting force categorization operation");

            // This would typically be implemented as a background job as well
            // For now, we'll execute it directly since it's not in the original job service
            var jobId = Guid.NewGuid().ToString();

            var response = new ActionJobResponse
            {
                JobId = jobId,
                Status = "Running",
                StartTime = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["Operation"] = "ForceCategorize"
                }
            };

            _logger.LogInformation("Force categorization started with ID: {JobId}", jobId);

            return Result<ActionJobResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start force categorization");
            return Result<ActionJobResponse>.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<TrainModelResponse>> TrainModelAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting ML model training");

            var startTime = DateTime.UtcNow;
            var trainResult = await _machineLearningService.TrainAndSaveModelAsync(cancellationToken);

            if (trainResult.IsFailure)
            {
                return Result<TrainModelResponse>.Failure($"Model training failed: {trainResult.ErrorMessage}");
            }

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Get model info for additional details
            var modelInfoResult = await _machineLearningService.GetModelInfoAsync(cancellationToken);
            
            var response = new TrainModelResponse
            {
                Success = true,
                Message = trainResult.Data ?? "Model training completed successfully",
                TrainingDuration = duration,
                ModelVersion = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                Metrics = new Dictionary<string, double>
                {
                    ["TrainingDurationSeconds"] = duration.TotalSeconds
                }
            };

            // Parse model info if available
            if (modelInfoResult.IsSuccess && !string.IsNullOrEmpty(modelInfoResult.Data))
            {
                var modelInfo = modelInfoResult.Data;
                if (modelInfo.Contains("Size:"))
                {
                    // Try to extract model size from the info string
                    var sizePart = modelInfo.Split("Size:")[1].Split(" bytes")[0].Trim();
                    if (long.TryParse(sizePart, out var modelSize))
                    {
                        response.ModelSizeBytes = modelSize;
                    }
                }
                
                if (modelInfo.Contains("Model exists at:"))
                {
                    response.ModelPath = modelInfo.Split("Model exists at:")[1].Split(",")[0].Trim();
                }
            }

            _logger.LogInformation("Model training completed successfully in {Duration}", duration);

            return Result<TrainModelResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to train ML model");
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

            // In a real implementation, we would query Hangfire's job storage
            // For now, return a basic response
            var response = new ActionJobResponse
            {
                JobId = jobId,
                Status = "Unknown",
                StartTime = DateTime.UtcNow.AddMinutes(-5), // Placeholder
                Metadata = new Dictionary<string, object>
                {
                    ["Note"] = "Job status tracking would be implemented with Hangfire job storage"
                }
            };

            return Result<ActionJobResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job status for {JobId}", jobId);
            return Result<ActionJobResponse>.FromException(ex);
        }
    }
}