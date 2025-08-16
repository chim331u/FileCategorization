using FileCategorization_Api.Common;
using FileCategorization_Shared.Common;
using FileCategorization_Api.Contracts.Actions;
using FileCategorization_Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FileCategorization_Api.Endpoints;

/// <summary>
/// Modern v2 endpoints for actions operations with Repository Pattern and Result Pattern.
/// Provides comprehensive validation, structured responses, and enhanced error handling.
/// </summary>
public static class ActionsEndpointV2
{
    /// <summary>
    /// Maps the v2 action-related endpoints to the specified <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">The <see cref="IEndpointRouteBuilder"/> to map the endpoints to.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> with the mapped endpoints.</returns>
    public static IEndpointRouteBuilder MapActionsV2Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2/actions")
            .WithTags("Actions v2")
            .WithOpenApi();

        /// <summary>
        /// Endpoint to refresh files from the origin directory with enhanced configuration.
        /// Scans for new files, categorizes them using ML, and adds them to the database in optimized batches.
        /// </summary>
        group.MapPost("/refresh-files", async (
            [FromBody] RefreshFilesRequest request,
            IActionsService actionsService,
            CancellationToken cancellationToken) =>
        {
            var result = await actionsService.RefreshFilesAsync(request, cancellationToken);
            
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(new { Error = result.Error });
        })
        .WithName("RefreshFilesV2")
        .WithSummary("Refresh files from origin directory with ML categorization")
        .WithDescription("Scans the configured origin directory for new files, " +
                        "categorizes them using machine learning, and adds them to the database. " +
                        "Supports batch processing and filtering options for optimal performance.")
        .Produces<ActionJobResponse>(200, "application/json")
        .ProducesProblem(400)
        .AddEndpointFilter<ValidationFilter<RefreshFilesRequest>>();

        /// <summary>
        /// Endpoint to move files to their categorized directories with comprehensive validation.
        /// Supports batch operations, error handling, and progress tracking.
        /// </summary>
        group.MapPost("/move-files", async (
            [FromBody] MoveFilesRequest request,
            IActionsService actionsService,
            CancellationToken cancellationToken) =>
        {
            var result = await actionsService.MoveFilesAsync(request, cancellationToken);
            
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(new { Error = result.Error });
        })
        .WithName("MoveFilesV2")
        .WithSummary("Move files to categorized directories")
        .WithDescription("Moves files from the origin directory to their categorized destination directories. " +
                        "Updates database records, appends training data, and provides real-time progress tracking. " +
                        "Supports batch operations for optimal performance.")
        .Produces<ActionJobResponse>(200, "application/json")
        .ProducesProblem(400)
        .AddEndpointFilter<ValidationFilter<MoveFilesRequest>>();

        /// <summary>
        /// Endpoint to force re-categorization of uncategorized files.
        /// Uses the current ML model to predict categories for files marked as needing categorization.
        /// </summary>
        group.MapPost("/force-categorize", async (
            IActionsService actionsService,
            CancellationToken cancellationToken) =>
        {
            var result = await actionsService.ForceCategorizeAsync(cancellationToken);
            
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(new { Error = result.Error });
        })
        .WithName("ForceCategorizeV2")
        .WithSummary("Force re-categorization of uncategorized files")
        .WithDescription("Forces re-categorization of all files marked as needing categorization. " +
                        "Uses the current machine learning model to predict categories and updates the database.")
        .Produces<ActionJobResponse>(200, "application/json")
        .ProducesProblem(400);

        /// <summary>
        /// Endpoint to train and save a new machine learning model with detailed metrics.
        /// Provides comprehensive information about the training process and model performance.
        /// </summary>
        group.MapPost("/train-model", async (
            IActionsService actionsService,
            CancellationToken cancellationToken) =>
        {
            var result = await actionsService.TrainModelAsync(cancellationToken);
            
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(new { Error = result.Error });
        })
        .WithName("TrainModelV2")
        .WithSummary("Train and save machine learning model")
        .WithDescription("Trains a new machine learning model using the current training data " +
                        "and saves it for future file categorization. Returns detailed training metrics and model information.")
        .Produces<TrainModelResponse>(200, "application/json")
        .ProducesProblem(400);

        /// <summary>
        /// Endpoint to get the status and progress of a background job.
        /// Provides real-time information about job execution, progress, and any errors.
        /// </summary>
        group.MapGet("/jobs/{jobId}/status", async (
            string jobId,
            IActionsService actionsService,
            CancellationToken cancellationToken) =>
        {
            var result = await actionsService.GetJobStatusAsync(jobId, cancellationToken);
            
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(new { Error = result.Error });
        })
        .WithName("GetJobStatusV2")
        .WithSummary("Get background job status and progress")
        .WithDescription("Retrieves the current status, progress, and detailed information " +
                        "about a background job, including completion percentage and any errors.")
        .Produces<ActionJobResponse>(200, "application/json")
        .ProducesProblem(400);

        return app;
    }
}