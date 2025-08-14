using Microsoft.AspNetCore.Mvc;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Contracts.DD;
using FileCategorization_Api.Common;

namespace FileCategorization_Api.Endpoints;

/// <summary>
/// Modern v2 endpoints for DD operations with comprehensive validation and error handling
/// </summary>
public static class DDEndpointV2
{
    /// <summary>
    /// Maps DD v2 endpoints to the application
    /// </summary>
    /// <param name="app">The endpoint route builder</param>
    /// <returns>The endpoint route builder with mapped DD v2 endpoints</returns>
    public static IEndpointRouteBuilder MapDDEndpointsV2(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2/dd")
            .WithTags("DD v2")
            .WithOpenApi();

        // Thread processing endpoints
        group.MapPost("/threads/process", ProcessThread)
            .WithName("ProcessDDThread")
            .WithSummary("Process DD thread by URL")
            .WithDescription("Processes a DD thread by URL, extracting and storing thread information and ED2K links. Creates new thread if it doesn't exist, or updates existing thread.")
            .Produces<ThreadProcessingResultDto>(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(500)
            .AddEndpointFilter<ValidationFilter<ProcessThreadRequestDto>>();

        group.MapPost("/threads/{threadId:int}/refresh", RefreshThreadLinks)
            .WithName("RefreshDDThreadLinks")
            .WithSummary("Refresh links for existing thread")
            .WithDescription("Refreshes ED2K links for an existing thread by re-parsing the thread's URL.")
            .Produces<ThreadProcessingResultDto>(200)
            .Produces<ProblemDetails>(404)
            .Produces<ProblemDetails>(500);

        // Thread query endpoints
        group.MapGet("/threads", GetActiveThreads)
            .WithName("GetActiveDDThreads")
            .WithSummary("Get all active DD threads")
            .WithDescription("Retrieves all active DD threads with link statistics including total links, new links, and used links counts.")
            .Produces<List<ThreadSummaryDto>>(200)
            .Produces<ProblemDetails>(500);

        group.MapGet("/threads/{threadId:int}/links", GetThreadLinks)
            .WithName("GetDDThreadLinks")
            .WithSummary("Get links for specific thread")
            .WithDescription("Retrieves all ED2K links for a specific thread. Optionally filter to exclude used links.")
            .Produces<List<LinkDto>>(200)
            .Produces<ProblemDetails>(404)
            .Produces<ProblemDetails>(500);

        // Link management endpoints
        group.MapPost("/links/{linkId:int}/use", UseLink)
            .WithName("UseDDLink")
            .WithSummary("Mark link as used")
            .WithDescription("Marks an ED2K link as used and returns the link information. This is typically called when a link is sent to download.")
            .Produces<LinkUsageResultDto>(200)
            .Produces<ProblemDetails>(404)
            .Produces<ProblemDetails>(500);

        // Thread management endpoints
        group.MapDelete("/threads/{threadId:int}", DeactivateThread)
            .WithName("DeactivateDDThread")
            .WithSummary("Deactivate thread")
            .WithDescription("Deactivates a DD thread and all its associated links. This is a soft delete operation.")
            .Produces<bool>(200)
            .Produces<ProblemDetails>(404)
            .Produces<ProblemDetails>(500);

        return app;
    }

    /// <summary>
    /// Processes a DD thread by URL
    /// </summary>
    private static async Task<IResult> ProcessThread(
        [FromBody] ProcessThreadRequestDto request,
        [FromServices] IDDQueryService ddService,
        [FromServices] ILogger<IDDQueryService> logger)
    {
        logger.LogInformation("Processing DD thread request for URL: {Url}", request.ThreadUrl);

        var result = await ddService.ProcessThreadAsync(request.ThreadUrl);
        
        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to process thread {Url}: {Error}", request.ThreadUrl, result.ErrorMessage);
            return Results.Problem(
                title: "Thread Processing Failed",
                detail: result.ErrorMessage,
                statusCode: 400);
        }

        logger.LogInformation("Successfully processed thread {ThreadId} with {NewLinks} new links", 
            result.Data.ThreadId, result.Data.NewLinksCount);

        return Results.Ok(result.Data);
    }

    /// <summary>
    /// Refreshes links for an existing thread
    /// </summary>
    private static async Task<IResult> RefreshThreadLinks(
        [FromRoute] int threadId,
        [FromServices] IDDQueryService ddService,
        [FromServices] ILogger<IDDQueryService> logger)
    {
        logger.LogInformation("Refreshing links for thread: {ThreadId}", threadId);

        var result = await ddService.RefreshThreadLinksAsync(threadId);
        
        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to refresh thread {ThreadId}: {Error}", threadId, result.ErrorMessage);
            
            if (result.ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem(
                    title: "Thread Not Found",
                    detail: result.ErrorMessage,
                    statusCode: 404);
            }

            return Results.Problem(
                title: "Thread Refresh Failed",
                detail: result.ErrorMessage,
                statusCode: 500);
        }

        logger.LogInformation("Successfully refreshed thread {ThreadId} with {NewLinks} new links", 
            threadId, result.Data.NewLinksCount);

        return Results.Ok(result.Data);
    }

    /// <summary>
    /// Gets all active threads
    /// </summary>
    private static async Task<IResult> GetActiveThreads(
        [FromServices] IDDQueryService ddService,
        [FromServices] ILogger<IDDQueryService> logger)
    {
        logger.LogInformation("Getting active DD threads");

        var result = await ddService.GetActiveThreadsAsync();
        
        if (!result.IsSuccess)
        {
            logger.LogError("Failed to get active threads: {Error}", result.ErrorMessage);
            return Results.Problem(
                title: "Failed to Get Threads",
                detail: result.ErrorMessage,
                statusCode: 500);
        }

        logger.LogInformation("Successfully retrieved {Count} active threads", result.Data.Count);
        return Results.Ok(result.Data);
    }

    /// <summary>
    /// Gets links for a specific thread
    /// </summary>
    private static async Task<IResult> GetThreadLinks(
        [FromRoute] int threadId,
        [FromServices] IDDQueryService ddService,
        [FromServices] ILogger<IDDQueryService> logger,
        [FromQuery] bool includeUsed = true)
    {
        logger.LogInformation("Getting links for thread: {ThreadId}, includeUsed: {IncludeUsed}", threadId, includeUsed);

        var result = await ddService.GetThreadLinksAsync(threadId, includeUsed);
        
        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to get links for thread {ThreadId}: {Error}", threadId, result.ErrorMessage);
            return Results.Problem(
                title: "Failed to Get Thread Links",
                detail: result.ErrorMessage,
                statusCode: 500);
        }

        logger.LogInformation("Successfully retrieved {Count} links for thread {ThreadId}", 
            result.Data.Count, threadId);

        return Results.Ok(result.Data);
    }

    /// <summary>
    /// Marks a link as used
    /// </summary>
    private static async Task<IResult> UseLink(
        [FromRoute] int linkId,
        [FromServices] IDDQueryService ddService,
        [FromServices] ILogger<IDDQueryService> logger)
    {
        logger.LogInformation("Marking link as used: {LinkId}", linkId);

        var result = await ddService.UseLink(linkId);
        
        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to use link {LinkId}: {Error}", linkId, result.ErrorMessage);
            
            if (result.ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem(
                    title: "Link Not Found",
                    detail: result.ErrorMessage,
                    statusCode: 404);
            }

            return Results.Problem(
                title: "Failed to Use Link",
                detail: result.ErrorMessage,
                statusCode: 500);
        }

        logger.LogInformation("Successfully marked link {LinkId} as used", linkId);
        return Results.Ok(result.Data);
    }

    /// <summary>
    /// Deactivates a thread
    /// </summary>
    private static async Task<IResult> DeactivateThread(
        [FromRoute] int threadId,
        [FromServices] IDDQueryService ddService,
        [FromServices] ILogger<IDDQueryService> logger)
    {
        logger.LogInformation("Deactivating thread: {ThreadId}", threadId);

        var result = await ddService.DeactivateThreadAsync(threadId);
        
        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to deactivate thread {ThreadId}: {Error}", threadId, result.ErrorMessage);
            
            if (result.ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem(
                    title: "Thread Not Found",
                    detail: result.ErrorMessage,
                    statusCode: 404);
            }

            return Results.Problem(
                title: "Failed to Deactivate Thread",
                detail: result.ErrorMessage,
                statusCode: 500);
        }

        logger.LogInformation("Successfully deactivated thread {ThreadId}", threadId);
        return Results.Ok(result.Data);
    }
}