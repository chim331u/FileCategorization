using Microsoft.AspNetCore.Mvc;

namespace FileCategorization_Api.Endpoints;

/// <summary>
/// Extension methods for mapping healthcheck endpoints.
/// </summary>
public static class HealthcheckEndpoint
{
    /// <summary>
    /// Maps the healthcheck endpoints to the specified route group.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <returns>The route group builder for chaining.</returns>
    public static RouteGroupBuilder MapHealthcheckEndPoints(this RouteGroupBuilder group)
    {
        // Get service status/healthcheck
        group.MapGet("/healthcheck", GetHealthcheckAsync)
            .WithName("GetHealthcheck_v2")
            .WithSummary("[v2] Gets service health status")
            .WithDescription("[v2] Verifies service availability and health status")
            .Produces<HealthcheckResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status503ServiceUnavailable);

        return group;
    }

    /// <summary>
    /// Gets the service health status.
    /// </summary>
    private static async Task<IResult> GetHealthcheckAsync(
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing healthcheck request");

        try
        {
            var response = new HealthcheckResponse
            {
                Status = "Healthy",
                Service = "FileCategorization API",
                Version = "v2",
                Timestamp = DateTime.UtcNow,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            };

            logger.LogInformation("Healthcheck completed successfully");
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Healthcheck failed");
            
            var errorResponse = new HealthcheckResponse
            {
                Status = "Unhealthy",
                Service = "FileCategorization API",
                Version = "v2",
                Timestamp = DateTime.UtcNow,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                Error = ex.Message
            };

            return Results.Json(errorResponse, statusCode: 503);
        }
    }
}

/// <summary>
/// Response model for healthcheck endpoint.
/// </summary>
public class HealthcheckResponse
{
    /// <summary>
    /// Health status of the service.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Service name.
    /// </summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// API version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the healthcheck.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Current environment.
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Error message if unhealthy.
    /// </summary>
    public string? Error { get; set; }
}