using FileCategorization_Api.Services;
using FileCategorization_Api.Domain.Entities.FilesDetail;
using FileCategorization_Api.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FileCategorization_Api.Endpoints;

/// <summary>
/// Extension methods for mapping file query endpoints.
/// </summary>
public static class FilesQueryEndpoint
{
    /// <summary>
    /// Maps the file query endpoints to the specified route group.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <returns>The route group builder for chaining.</returns>
    public static RouteGroupBuilder MapFilesQueryEndPoints(this RouteGroupBuilder group)
    {
        // Get file categories
        group.MapGet("/categories", GetCategoriesAsync)
            .WithName("GetFileCategories_v2")
            .WithSummary("[v2] Gets all distinct file categories")
            .WithDescription("[v2 - Repository Pattern] Retrieves a list of all distinct file categories available in the system")
            .Produces<IEnumerable<string>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

        // Get filtered files  
        group.MapGet("/files/filtered/{filterType:int}", GetFilteredFilesAsync)
            .WithName("GetFilteredFiles_v2")
            .WithSummary("[v2] Gets files filtered by type")
            .WithDescription("[v2 - Repository Pattern] Retrieves files based on filter type: 1=All, 2=Categorized, 3=ToCategorize, 4=New")
            .Produces<IEnumerable<FilesDetailResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        // Get last view list
        group.MapGet("/files/lastview", GetLastViewAsync)
            .WithName("GetLastViewFiles_v2")
            .WithSummary("[v2] Gets the latest file from each category")
            .WithDescription("[v2 - Repository Pattern] Retrieves the most recent file for each category for the last view functionality")
            .Produces<IEnumerable<FilesDetailResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

        // Get files to categorize
        group.MapGet("/files/tocategorize", GetFilesToCategorizeAsync)
            .WithName("GetFilesToCategorize")
            .WithSummary("Gets files that need categorization")
            .WithDescription("Retrieves all files that are marked as needing categorization")
            .Produces<IEnumerable<FilesDetailResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

        // Search files by name
        group.MapGet("/files/search", SearchFilesAsync)
            .WithName("SearchFiles_v2")
            .WithSummary("[v2] Search files by name pattern")
            .WithDescription("[v2 - Repository Pattern] Searches for files that match the provided name pattern")
            .Produces<IEnumerable<FilesDetailResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        return group;
    }

    /// <summary>
    /// Gets all distinct file categories.
    /// </summary>
    private static async Task<IResult> GetCategoriesAsync(
        [FromServices] IFilesQueryService filesQueryService,
        CancellationToken cancellationToken)
    {
        var result = await filesQueryService.GetCategoriesAsync(cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : Results.Problem(result.ErrorMessage, statusCode: 500);
    }

    /// <summary>
    /// Gets files filtered by the specified criteria.
    /// </summary>
    private static async Task<IResult> GetFilteredFilesAsync(
        [FromRoute] int filterType,
        [FromServices] IFilesQueryService filesQueryService,
        CancellationToken cancellationToken)
    {
        // Validate filter type
        if (!Enum.IsDefined(typeof(FileFilterType), filterType))
        {
            return Results.BadRequest($"Invalid filter type. Valid values are: {string.Join(", ", Enum.GetValues<FileFilterType>().Cast<int>())}");
        }

        var result = await filesQueryService.GetFilteredFilesAsync((FileFilterType)filterType, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : Results.Problem(result.ErrorMessage, statusCode: 500);
    }

    /// <summary>
    /// Gets the latest file from each category.
    /// </summary>
    private static async Task<IResult> GetLastViewAsync(
        [FromServices] IFilesQueryService filesQueryService,
        CancellationToken cancellationToken)
    {
        var result = await filesQueryService.GetLastViewListAsync(cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : Results.Problem(result.ErrorMessage, statusCode: 500);
    }

    /// <summary>
    /// Gets files that need to be categorized.
    /// </summary>
    private static async Task<IResult> GetFilesToCategorizeAsync(
        [FromServices] IFilesQueryService filesQueryService,
        CancellationToken cancellationToken)
    {
        var result = await filesQueryService.GetFilesToCategorizeAsync(cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : Results.Problem(result.ErrorMessage, statusCode: 500);
    }

    /// <summary>
    /// Searches files by name pattern.
    /// </summary>
    private static async Task<IResult> SearchFilesAsync(
        [FromQuery] string pattern,
        [FromServices] IFilesQueryService filesQueryService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return Results.BadRequest("Search pattern is required");
        }

        var result = await filesQueryService.SearchFilesByNameAsync(pattern, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : Results.Problem(result.ErrorMessage, statusCode: 500);
    }
}