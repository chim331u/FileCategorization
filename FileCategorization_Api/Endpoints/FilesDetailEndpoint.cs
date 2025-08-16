using FileCategorization_Api.Domain.Entities.FilesDetail;
using FileCategorization_Shared.DTOs.FileManagement;
using FileCategorization_Api.Interfaces;
using System.ComponentModel;

namespace FileCategorization_Api.Endpoints;

/// <summary>
/// Provides extension methods to map file detail-related endpoints.
/// </summary>
public static class FilesDetailEndPoint
{
    /// <summary>
    /// Maps the file detail-related endpoints to the specified <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">The <see cref="IEndpointRouteBuilder"/> to map the endpoints to.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> with the mapped endpoints.</returns>
    public static IEndpointRouteBuilder MapFilesDetailEndPoint(this IEndpointRouteBuilder app)
    {
        // Define the endpoints

        /// <summary>
        /// Endpoint to get a list of categories.
        /// </summary>
        /// <param name="filesDetailService">The service to retrieve the categories.</param>
        app.MapGet("/CategoryList", async (IFilesDetailService filesDetailService) =>
        {
            var result = await filesDetailService.GetDbCategoryList();
            return Results.Ok(result);
        })
        .WithSummary("[OBSOLETE] Get list of categories")
        .WithDescription("⚠️ **DEPRECATED**: This endpoint is obsolete. Use `GET /api/v2/files/categories` instead for improved performance and structured responses.")
        .WithMetadata(new ObsoleteAttribute("This endpoint is deprecated. Use GET /api/v2/categories instead.", false));

        /// <summary>
        /// Endpoint to get all file details.
        /// </summary>
        /// <param name="filesDetailService">The service to retrieve the file details.</param>
        app.MapGet("/filesDetailList", async (IFilesDetailService filesDetailService) =>
        {
            var result = await filesDetailService.GetFileList();
            return Results.Ok(result);
        })
        .WithSummary("[OBSOLETE] Get all file details")
        .WithDescription("⚠️ **DEPRECATED**: This endpoint is obsolete. Use `GET /api/v2/files/list` instead for paginated results and better performance.")
        .WithMetadata(new ObsoleteAttribute("This endpoint is deprecated. Use GET /api/v2/files/filtered/1 instead.", false));

        /// <summary>
        /// Endpoint to get a filtered list of file details.
        /// </summary>
        /// <param name="searchPar">The search parameter to filter the file list.</param>
        /// <param name="filesDetailService">The service to retrieve the file details.</param>
        app.MapGet("/GetFileList/{searchPar}", async (string searchPar, IFilesDetailService filesDetailService) =>
        {
            var fileList = await filesDetailService.GetFileList();

            switch (searchPar)
            {
                case "3": // To categorize
                    fileList = fileList.Where(x => x.IsToCategorize).ToList();
                    break;

                case "2": // Categorized
                    fileList = fileList.Where(x => !x.IsToCategorize).ToList();
                    break;

                default:
                    break;
            }

            return Results.Ok(fileList);
        })
        .WithSummary("[OBSOLETE] Get filtered file list")
        .WithDescription("⚠️ **DEPRECATED**: This endpoint is obsolete. Use `GET /api/v2/files/filtered/{filterType}` instead for structured filtering with better performance.")
        .WithMetadata(new ObsoleteAttribute("This endpoint is deprecated. Use GET /api/v2/files/filtered/{filterType} or GET /api/v2/files/search instead.", false));

        /// <summary>
        /// Endpoint to get file details by ID.
        /// </summary>
        /// <param name="id">The unique identifier of the file detail.</param>
        /// <param name="filesDetailService">The service to retrieve the file detail.</param>
        app.MapGet("/GetFilesDetail/{id:int}", async (int id, IFilesDetailService filesDetailService) =>
        {
            var result = await filesDetailService.GetFilesDetailById(id);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithSummary("[OBSOLETE] Get file details by ID")
        .WithDescription("⚠️ **DEPRECATED**: This endpoint is obsolete. Use `GET /api/v2/files/{id}` instead for consistent API design and better error handling.");

        /// <summary>
        /// Endpoint to get files to move.
        /// </summary>
        /// <param name="filesDetailService">The service to retrieve the files to move.</param>
        app.MapGet("/GetFileToMove", async (IFilesDetailService filesDetailService) =>
        {
            var result = await filesDetailService.GetFileListToCategorize();
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithSummary("[OBSOLETE] Get files to move")
        .WithDescription("⚠️ **DEPRECATED**: This endpoint is obsolete. Use `GET /api/v2/files/filtered/uncategorized` instead for better filtering and performance.");

        /// <summary>
        /// Endpoint to add a new file detail.
        /// </summary>
        /// <param name="filesDetailRequest">The file detail data to add.</param>
        /// <param name="filesDetailService">The service to process the request.</param>
        app.MapPost("/AddFilesDetail", async (FilesDetailRequest filesDetailRequest, IFilesDetailService filesDetailService) =>
        {
            var result = await filesDetailService.AddFileDetailAsync(filesDetailRequest);
            return Results.Created($"/GetFilesDetail/{result.Id}", result);
        })
        .WithSummary("[OBSOLETE] Add new file detail")
        .WithDescription("⚠️ **DEPRECATED**: This endpoint is obsolete. Use `POST /api/v2/files` instead for improved validation and structured responses.")
        .WithMetadata(new ObsoleteAttribute("This endpoint is deprecated. Use POST /api/v2/files instead.", false));

        /// <summary>
        /// Endpoint to update an existing file detail.
        /// </summary>
        /// <param name="id">The unique identifier of the file detail to update.</param>
        /// <param name="filesDetailUpdateRequest">The updated file detail data.</param>
        /// <param name="filesDetailService">The service to process the request.</param>
        app.MapPut("/UpdateFilesDetail/{id:int}", async (int id, FilesDetailUpdateRequest filesDetailUpdateRequest, IFilesDetailService filesDetailService) =>
        {
            var result = await filesDetailService.UpdateFilesDetailAsync(id, filesDetailUpdateRequest);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithSummary("[OBSOLETE] Update file details")
        .WithDescription("⚠️ **DEPRECATED**: This endpoint is obsolete. Use `PUT /api/v2/files/{id}` instead for improved validation and structured error responses.")
        .WithMetadata(new ObsoleteAttribute("This endpoint is deprecated. Use PUT /api/v2/files/{id} instead.", false));

        /// <summary>
        /// Endpoint to delete a file detail by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the file detail to delete.</param>
        /// <param name="filesDetailService">The service to process the request.</param>
        app.MapDelete("/DeleteFilesDetail/{id:int}", async (int id, IFilesDetailService filesDetailService) =>
        {
            var result = await filesDetailService.DeleteFilesDetailAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithSummary("[OBSOLETE] Delete file detail")
        .WithDescription("⚠️ **DEPRECATED**: This endpoint is obsolete. Use `DELETE /api/v2/files/{id}` instead for consistent API design and better error handling.")
        .WithMetadata(new ObsoleteAttribute("This endpoint is deprecated. Use DELETE /api/v2/files/{id} instead.", false));

        /// <summary>
        /// Endpoint to get the last viewed file details.
        /// </summary>
        /// <param name="filesDetailService">The service to retrieve the last viewed file details.</param>
        app.MapGet("/GetLastViewList", async (IFilesDetailService filesDetailService) =>
        {
            var result = await filesDetailService.GetLastViewList();
            return Results.Ok(result);
        })
        .WithSummary("[OBSOLETE] Get last viewed files")
        .WithDescription("⚠️ **DEPRECATED**: This endpoint is obsolete. Use `GET /api/v2/files/recent` instead for paginated results and better performance.")
        .WithMetadata(new ObsoleteAttribute("This endpoint is deprecated. Use GET /api/v2/files/lastview instead.", false));

        /// <summary>
        /// Endpoint to get all files by category.
        /// </summary>
        /// <param name="cat">The category to filter the files.</param>
        /// <param name="filesDetailService">The service to retrieve the files.</param>
        app.MapGet("/GetAllFiles/{cat}", async (string cat, IFilesDetailService filesDetailService) =>
        {
            var result = await filesDetailService.GetAllFiles(cat);
            return Results.Ok(result);
        })
        .WithSummary("[OBSOLETE] Get all files by category")
        .WithDescription("⚠️ **DEPRECATED**: This endpoint is obsolete. Use `GET /api/v2/files/category/{category}` instead for paginated results and better performance.");

        return app;
    }
}