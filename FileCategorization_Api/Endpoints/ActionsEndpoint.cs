using FileCategorization_Shared.DTOs.FileManagement;
using FileCategorization_Api.Interfaces;
using Hangfire;

namespace FileCategorization_Api.Endpoints;

/// <summary>
/// Provides extension methods to map action-related endpoints.
/// </summary>
public static class ActionsEndpoint
{
    /// <summary>
    /// Maps the action-related endpoints to the specified <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">The <see cref="IEndpointRouteBuilder"/> to map the endpoints to.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> with the mapped endpoints.</returns>
    public static IEndpointRouteBuilder MapActionsEndPoint(this IEndpointRouteBuilder app)
    {
        // Define the endpoints
            
        /// <summary>
        /// Endpoint to update files from the directory to the database.
        /// </summary>
        app.MapGet("/RefreshFiles", async (IHangFireJobService jobService) =>
        {
            string jobId =
                BackgroundJob.Enqueue<IHangFireJobService>(job =>
                    job.RefreshFiles(CancellationToken.None));
            
            return Results.Ok(jobId);
        })
        .WithSummary("[OBSOLETE] Refresh files from directory")
        .WithDescription("⚠️ **DEPRECATED**: This endpoint is obsolete. Use '/api/v2/actions/refresh-files' instead for improved performance, validation, and structured responses.");
        
        /// <summary>
        /// Endpoint to move files from the original directory to the destination directory.
        /// </summary>
        app.MapPost("/MoveFiles", async (List<FileMoveDto> filetToMoveList, IHangFireJobService jobService) =>
        {
            if (filetToMoveList == null || filetToMoveList.Count < 1)
            {
                return Results.BadRequest("No files to move");
            }
            string jobId =
                BackgroundJob.Enqueue<IHangFireJobService>(job =>
                    job.MoveFilesJob(filetToMoveList, CancellationToken.None));
            
            return Results.Ok(jobId);
        })
        .WithSummary("[OBSOLETE] Move files to categorized directories")
        .WithDescription("⚠️ **DEPRECATED**: This endpoint is obsolete. Use '/api/v2/actions/move-files' instead for improved validation, batch operations, and comprehensive error handling.");
        
        /// <summary>
        /// Endpoint to force categorization of files.
        /// </summary>
        app.MapGet("/ForceCategory", async (IFilesDetailService filesDetailService) =>
        {
            string forceCategoryResult = await filesDetailService.ForceCategory();
            return Results.Ok(forceCategoryResult);
        })
        .WithSummary("[OBSOLETE] Force categorization of files")
        .WithDescription("⚠️ **DEPRECATED**: This endpoint is obsolete. Use '/api/v2/actions/force-categorize' instead for better async patterns and structured error handling.");
        
        /// <summary>
        /// Endpoint to train and save a machine learning model.
        /// </summary>
        app.MapGet("/TrainModel", async (IMachineLearningService machineLearningService) =>
        {
            var trainModelResult = await machineLearningService.TrainAndSaveModelAsync();
            
            if (trainModelResult.IsFailure)
            {
                return Results.BadRequest(trainModelResult.Error);
            }
            
            return Results.Ok(trainModelResult.Value);
        })
        .WithSummary("[OBSOLETE] Train machine learning model")
        .WithDescription("⚠️ **DEPRECATED**: This endpoint is obsolete. Use '/api/v2/actions/train-model' instead for detailed training metrics, model information, and better error handling.");
        
        return app;
    }
}