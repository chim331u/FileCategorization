using System.Net;
using FileCategorization_Api.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace FileCategorization_Api.Endpoints;

/// <summary>
/// Provides extension methods to map DD-related endpoints.
/// </summary>
public static class DDEndpoint
{
    /// <summary>
    /// Maps the DD-related endpoints to the specified <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">The <see cref="IEndpointRouteBuilder"/> to map the endpoints to.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> with the mapped endpoints.</returns>
    public static IEndpointRouteBuilder MapDDEndPoint(this IEndpointRouteBuilder app)
    {
        // Define the endpoints

        /// <summary>
        /// Endpoint to check a link and retrieve associated data.
        /// </summary>
        /// <param name="link">The encoded link to check.</param>
        /// <param name="_service">The service to process the link.</param>
        app.MapGet("/CheckLink/{link}", async (string link, IDDService _service) =>
        {
            var decodedStr = Base64UrlEncoder.Decode(link);
            decodedStr = WebUtility.UrlDecode(decodedStr);
            decodedStr = WebUtility.HtmlDecode(decodedStr);

            string? result = await _service.GetLinks(decodedStr);

            if (string.IsNullOrEmpty(result))
            {
                return Results.NotFound();
            }

            return Results.Ok(result);
        });

        /// <summary>
        /// Endpoint to check links associated with a thread ID.
        /// </summary>
        /// <param name="threadId">The thread ID to check.</param>
        /// <param name="_service">The service to process the thread ID.</param>
        app.MapGet("/CheckLinks/{threadId}", async (int threadId, IDDService _service) =>
        {
            string? result = await _service.GetLinks(threadId);

            if (string.IsNullOrEmpty(result))
            {
                return Results.NotFound();
            }

            return Results.Ok(result);
        });

        /// <summary>
        /// Endpoint to get active links by ID.
        /// </summary>
        /// <param name="id">The ID to retrieve active links for.</param>
        /// <param name="_service">The service to process the request.</param>
        app.MapGet("/GetLinks/{id}", async (int id, IDDService _service) =>
        {
            var result = await _service.GetActiveLinks(id);
            return Results.Ok(result);
        });

        /// <summary>
        /// Endpoint to mark a link as used by its ID.
        /// </summary>
        /// <param name="id">The ID of the link to mark as used.</param>
        /// <param name="_service">The service to process the request.</param>
        app.MapGet("/UseLink/{id}", async (int id, IDDService _service) =>
        {
            var result = await _service.UseLink(id);

            if (string.IsNullOrEmpty(result))
            {
                return Results.NotFound();
            }

            return Results.Ok(result);
        });

        /// <summary>
        /// Endpoint to get a list of active threads.
        /// </summary>
        /// <param name="_service">The service to retrieve active threads.</param>
        app.MapGet("/GetActiveThreads", async (IDDService _service) =>
        {
            var result = await _service.GetActiveThreads();
            return Results.Ok(result);
        });

        // /// <summary>
        // /// Endpoint to add a new DD setting.
        // /// </summary>
        // /// <param name="newSettingDto">The new setting data to add.</param>
        // /// <param name="_ddService">The service to process the request.</param>
        // app.MapPut("/AddDDSetting", async (DDSettingDto newSettingDto, IDDService _ddService) =>
        // {
        //     var result = await _ddService.AddSetting(newSettingDto);
        //     return Results.Ok(result);
        // });

        return app;
    }
}