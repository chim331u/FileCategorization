using AutoMapper;
using FileCategorization_Api.Contracts.FilesDetail;
using FileCategorization_Api.Core.Interfaces;
using FileCategorization_Api.Presentation.Filters;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FileCategorization_Api.Presentation.Endpoints;

/// <summary>
/// Extension methods for mapping file management endpoints.
/// </summary>
public static class FilesManagementEndpoint
{
    /// <summary>
    /// Maps the file management endpoints to the specified route group.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <returns>The route group builder for chaining.</returns>
    public static RouteGroupBuilder MapFilesManagementEndPoints(this RouteGroupBuilder group)
    {
        // Create new file
        group.MapPost("/files", CreateFileAsync)
            .WithName("CreateFile")
            .WithSummary("Creates a new file record")
            .WithDescription("Creates a new file record in the system with validation")
            .Produces<FilesDetailResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<FilesDetailRequest>>();

        // Update existing file
        group.MapPut("/files/{id:int}", UpdateFileAsync)
            .WithName("UpdateFile")
            .WithSummary("Updates an existing file record")
            .WithDescription("Updates an existing file record with validation")
            .Produces<FilesDetailResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<FilesDetailUpdateRequest>>();

        // Move file to category
        group.MapPatch("/files/move", MoveFileAsync)
            .WithName("MoveFile")
            .WithSummary("Moves a file to a different category")
            .WithDescription("Updates the file category with validation")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<FileMoveDto>>();

        // Delete file (soft delete)
        group.MapDelete("/files/{id:int}", DeleteFileAsync)
            .WithName("DeleteFile")
            .WithSummary("Soft deletes a file record")
            .WithDescription("Marks a file as deleted (soft delete)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        return group;
    }

    /// <summary>
    /// Creates a new file record.
    /// </summary>
    private static async Task<IResult> CreateFileAsync(
        [FromBody] FilesDetailRequest request,
        [FromServices] IFilesDetailRepository repository,
        [FromServices] IMapper mapper,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating new file record: {FileName}", request.Name);

        // Map request to entity
        var fileEntity = mapper.Map<Models.FileCategorization.FilesDetail>(request);
        
        // Add to repository
        var result = await repository.AddAsync(fileEntity, cancellationToken);
        
        if (result.IsFailure)
        {
            logger.LogError("Failed to create file record: {Error}", result.ErrorMessage);
            return Results.Problem(result.ErrorMessage, statusCode: 500);
        }

        // Map entity to response
        var response = mapper.Map<FilesDetailResponse>(result.Data);
        
        return Results.Created($"/api/v1/files/{response.Id}", response);
    }

    /// <summary>
    /// Updates an existing file record.
    /// </summary>
    private static async Task<IResult> UpdateFileAsync(
        [FromRoute] int id,
        [FromBody] FilesDetailUpdateRequest request,
        [FromServices] IFilesDetailRepository repository,
        [FromServices] IMapper mapper,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating file record: {FileId}", id);

        // Get existing file
        var existingResult = await repository.GetByIdAsync(id, cancellationToken);
        if (existingResult.IsFailure)
        {
            return Results.Problem(existingResult.ErrorMessage, statusCode: 500);
        }

        if (existingResult.Data == null)
        {
            return Results.NotFound($"File with ID {id} not found");
        }

        // Map update request to existing entity
        var updatedEntity = mapper.Map(request, existingResult.Data);
        
        // Update in repository
        var result = await repository.UpdateAsync(updatedEntity, cancellationToken);
        
        if (result.IsFailure)
        {
            logger.LogError("Failed to update file record {FileId}: {Error}", id, result.ErrorMessage);
            return Results.Problem(result.ErrorMessage, statusCode: 500);
        }

        // Map entity to response
        var response = mapper.Map<FilesDetailResponse>(result.Data);
        
        return Results.Ok(response);
    }

    /// <summary>
    /// Moves a file to a different category.
    /// </summary>
    private static async Task<IResult> MoveFileAsync(
        [FromBody] FileMoveDto request,
        [FromServices] IFilesDetailRepository repository,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Moving file {FileId} to category: {Category}", request.Id, request.FileCategory);

        // Update categorization
        var result = await repository.UpdateCategorizationAsync(
            request.Id, 
            request.FileCategory!, 
            false, // Mark as categorized
            cancellationToken);
        
        if (result.IsFailure)
        {
            logger.LogError("Failed to move file {FileId}: {Error}", request.Id, result.ErrorMessage);
            return Results.Problem(result.ErrorMessage, statusCode: 500);
        }

        if (!result.Data)
        {
            return Results.NotFound($"File with ID {request.Id} not found");
        }
        
        return Results.Ok(new { message = "File moved successfully", fileId = request.Id, category = request.FileCategory });
    }

    /// <summary>
    /// Soft deletes a file record.
    /// </summary>
    private static async Task<IResult> DeleteFileAsync(
        [FromRoute] int id,
        [FromServices] IFilesDetailRepository repository,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting file record: {FileId}", id);

        var result = await repository.DeleteAsync(id, cancellationToken);
        
        if (result.IsFailure)
        {
            logger.LogError("Failed to delete file record {FileId}: {Error}", id, result.ErrorMessage);
            return Results.Problem(result.ErrorMessage, statusCode: 500);
        }

        if (!result.Data)
        {
            return Results.NotFound($"File with ID {id} not found");
        }
        
        return Results.NoContent();
    }
}