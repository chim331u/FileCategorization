using FileCategorization_Api.Contracts.Configs;
using FileCategorization_Api.Interfaces;

namespace FileCategorization_Api.Endpoints;

/// <summary>
/// Provides extension methods to map configuration-related endpoints.
/// </summary>
public static class ConfigsEndpoint
{
    /// <summary>
    /// Maps the configuration-related endpoints to the specified <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">The <see cref="IEndpointRouteBuilder"/> to map the endpoints to.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> with the mapped endpoints.</returns>
    public static IEndpointRouteBuilder MapConfigsEndPoint(this IEndpointRouteBuilder app)
    {
        // Define the endpoints
            
        /// <summary>
        /// Endpoint to get a list of configurations.
        /// </summary>
        app.MapGet("/GetConfigList", async (IConfigsService configsService) =>
        {
            var result = await configsService.GetConfigList();
            return Results.Ok(result);
        });
        
        /// <summary>
        /// Endpoint to get a configuration by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the configuration.</param>
        app.MapGet("/GetConfig/{id:int}", async (int id, IConfigsService configsService) =>
        {
            var result = await configsService.GetConfig(id);
            return result != null ? Results.Ok(result) : Results.NotFound();
        });
        
        /// <summary>
        /// Endpoint to add a new configuration.
        /// </summary>
        /// <param name="configsDto">The configuration data to add.</param>
        app.MapPost("/AddConfig", async (ConfigsDto configsDto, IConfigsService configsService) =>
        {
            var result = await configsService.AddConfig(configsDto);
            return Results.Created($"/GetConfig/{result.Id}", result); 
        });
        
        /// <summary>
        /// Endpoint to update an existing configuration.
        /// </summary>
        /// <param name="id">The unique identifier of the configuration to update.</param>
        /// <param name="configsDto">The updated configuration data.</param>
        app.MapPut("/UpdateConfig/{id:int}", async (int id, ConfigsDto configsDto, IConfigsService configsService) =>
        {
            var result = await configsService.UpdateConfig(id, configsDto);
            return result != null ? Results.Ok(result) : Results.NotFound();
        });
        
        /// <summary>
        /// Endpoint to delete a configuration by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the configuration to delete.</param>
        app.MapDelete("/DeleteConfig/{id:int}", async (int id, IConfigsService configsService) =>
        {
            var result = await configsService.DeleteConfig(id);
            return result ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}