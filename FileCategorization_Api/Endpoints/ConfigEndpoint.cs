using AutoMapper;
using FileCategorization_Api.Common;
using FileCategorization_Api.Services;
using FileCategorization_Shared.DTOs.Configuration;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using FileCategorization_Shared.Common;
using Microsoft.AspNetCore.Mvc;

namespace FileCategorization_Api.Endpoints;

/// <summary>
/// Extension methods for mapping configuration endpoints.
/// </summary>
public static class ConfigEndpoint
{
    /// <summary>
    /// Maps the configuration endpoints to the specified route group.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <returns>The route group builder for chaining.</returns>
    public static RouteGroupBuilder MapConfigEndPoints(this RouteGroupBuilder group)
    {
        // Get all configurations
        group.MapGet("/configs", GetAllConfigsAsync)
            .WithName("GetAllConfigs_v2")
            .WithSummary("[v2] Gets all configuration settings")
            .WithDescription("[v2 - Repository Pattern] Retrieves all active configuration settings with structured response")
            .Produces<IEnumerable<ConfigResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

        // Get configuration by ID
        group.MapGet("/configs/{id:int}", GetConfigByIdAsync)
            .WithName("GetConfigById_v2")
            .WithSummary("[v2] Gets configuration by ID")
            .WithDescription("[v2 - Repository Pattern] Retrieves a specific configuration setting by its ID")
            .Produces<ConfigResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        // Get configuration by key
        group.MapGet("/configs/key/{key}", GetConfigByKeyAsync)
            .WithName("GetConfigByKey_v2")
            .WithSummary("[v2] Gets configuration by key")
            .WithDescription("[v2 - Repository Pattern] Retrieves a specific configuration setting by its key")
            .Produces<ConfigResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        // Get configuration value by key
        group.MapGet("/configs/value/{key}", GetConfigValueAsync)
            .WithName("GetConfigValue_v2")
            .WithSummary("[v2] Gets configuration value by key")
            .WithDescription("[v2 - Repository Pattern] Retrieves only the value of a configuration setting by its key")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        // Get configurations by environment
        group.MapGet("/configs/environment/{isDev:bool}", GetConfigsByEnvironmentAsync)
            .WithName("GetConfigsByEnvironment_v2")
            .WithSummary("[v2] Gets configurations by environment")
            .WithDescription("[v2 - Repository Pattern] Retrieves configuration settings filtered by environment (dev/prod)")
            .Produces<IEnumerable<ConfigResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

        // Create new configuration
        group.MapPost("/configs", CreateConfigAsync)
            .WithName("CreateConfig_v2")
            .WithSummary("[v2] Creates a new configuration")
            .WithDescription("[v2 - Repository Pattern] Creates a new configuration setting with validation")
            .Produces<ConfigResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<ConfigRequest>>();

        // Update existing configuration
        group.MapPut("/configs/{id:int}", UpdateConfigAsync)
            .WithName("UpdateConfig_v2")
            .WithSummary("[v2] Updates an existing configuration")
            .WithDescription("[v2 - Repository Pattern] Updates an existing configuration setting with validation")
            .Produces<ConfigResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<ConfigUpdateRequest>>();

        // Delete configuration (soft delete)
        group.MapDelete("/configs/{id:int}", DeleteConfigAsync)
            .WithName("DeleteConfig_v2")
            .WithSummary("[v2] Soft deletes a configuration")
            .WithDescription("[v2 - Repository Pattern] Marks a configuration setting as deleted (soft delete)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        return group;
    }

    /// <summary>
    /// Gets all configuration settings.
    /// </summary>
    private static async Task<IResult> GetAllConfigsAsync(
        [FromServices] IConfigQueryService configQueryService,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing request to get all configurations");

        var result = await configQueryService.GetAllConfigsAsync(cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 500);
    }

    /// <summary>
    /// Gets a configuration by its ID.
    /// </summary>
    private static async Task<IResult> GetConfigByIdAsync(
        [FromRoute] int id,
        [FromServices] IConfigQueryService configQueryService,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing request to get configuration by ID: {Id}", id);

        if (id <= 0)
        {
            return Results.BadRequest("Configuration ID must be greater than zero");
        }

        var result = await configQueryService.GetConfigByIdAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error, statusCode: 500);
        }

        return result.Value != null
            ? Results.Ok(result.Value)
            : Results.NotFound($"Configuration with ID {id} not found");
    }

    /// <summary>
    /// Gets a configuration by its key.
    /// </summary>
    private static async Task<IResult> GetConfigByKeyAsync(
        [FromRoute] string key,
        [FromServices] IConfigQueryService configQueryService,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing request to get configuration by key: {Key}", key);

        if (string.IsNullOrWhiteSpace(key))
        {
            return Results.BadRequest("Configuration key cannot be null or empty");
        }

        var result = await configQueryService.GetConfigByKeyAsync(key, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error, statusCode: 500);
        }

        return result.Value != null
            ? Results.Ok(result.Value)
            : Results.NotFound($"Configuration with key '{key}' not found");
    }

    /// <summary>
    /// Gets a configuration value by its key.
    /// </summary>
    private static async Task<IResult> GetConfigValueAsync(
        [FromRoute] string key,
        [FromServices] IConfigQueryService configQueryService,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing request to get configuration value by key: {Key}", key);

        if (string.IsNullOrWhiteSpace(key))
        {
            return Results.BadRequest("Configuration key cannot be null or empty");
        }

        var result = await configQueryService.GetConfigValueAsync(key, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error, statusCode: 500);
        }

        return result.Value != null
            ? Results.Ok(result.Value)
            : Results.NotFound($"Configuration with key '{key}' not found");
    }

    /// <summary>
    /// Gets configurations by environment.
    /// </summary>
    private static async Task<IResult> GetConfigsByEnvironmentAsync(
        [FromRoute] bool isDev,
        [FromServices] IConfigQueryService configQueryService,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing request to get configurations by environment. IsDev: {IsDev}", isDev);

        var result = await configQueryService.GetConfigsByEnvironmentAsync(isDev, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 500);
    }

    /// <summary>
    /// Creates a new configuration setting.
    /// </summary>
    private static async Task<IResult> CreateConfigAsync(
        [FromBody] ConfigRequest request,
        [FromServices] IConfigRepository configRepository,
        [FromServices] IMapper mapper,
        [FromServices] IHostEnvironment environment,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing request to create configuration with key: {Key}", request.Key);

        // Map request to entity
        var configEntity = mapper.Map<Configs>(request);
        
        // Set IsDev based on current environment instead of request value
        configEntity.IsDev = environment.IsDevelopment();
        
        logger.LogInformation("Setting IsDev to {IsDev} based on environment {EnvironmentName}", 
            configEntity.IsDev, environment.EnvironmentName);

        // Add to repository
        var result = await configRepository.AddAsync(configEntity, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("Failed to create configuration: {Error}", result.Error);
            
            // Check if it's a duplicate key error
            if (result.Error!.Contains("already exists"))
            {
                return Results.Conflict(result.Error);
            }
            
            return Results.Problem(result.Error, statusCode: 500);
        }

        // Map entity to response
        var response = mapper.Map<ConfigResponse>(result.Value);

        return Results.Created($"/api/v2/configs/{response.Id}", response);
    }

    /// <summary>
    /// Updates an existing configuration setting.
    /// </summary>
    private static async Task<IResult> UpdateConfigAsync(
        [FromRoute] int id,
        [FromBody] ConfigUpdateRequest request,
        [FromServices] IConfigRepository configRepository,
        [FromServices] IMapper mapper,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing request to update configuration: {Id}", id);

        // Get existing configuration
        var existingResult = await configRepository.GetByIdAsync(id, cancellationToken);
        if (existingResult.IsFailure)
        {
            return Results.Problem(existingResult.Error, statusCode: 500);
        }

        if (existingResult.Value == null)
        {
            return Results.NotFound($"Configuration with ID {id} not found");
        }

        // Update existing entity manually to preserve ID
        logger.LogInformation("Updating existing entity. Existing ID: {ExistingId}, Key: {ExistingKey}", 
            existingResult.Value.Id, existingResult.Value.Key);
        
        logger.LogInformation("Request data - Key: {RequestKey}, Value: {RequestValue}", 
            request.Key, request.Value);
            
        var updatedEntity = existingResult.Value;
        
        // Update only the fields that are provided
        if (!string.IsNullOrWhiteSpace(request.Key))
            updatedEntity.Key = request.Key;
            
        if (!string.IsNullOrWhiteSpace(request.Value))
            updatedEntity.Value = request.Value;
            
        // Note: IsDev cannot be changed after creation - it's determined by environment
            
        updatedEntity.LastUpdatedDate = DateTime.UtcNow;
        
        logger.LogInformation("After manual update - Entity ID: {EntityId}, Key: {EntityKey}, Value: {EntityValue}, IsDev: {EntityIsDev}", 
            updatedEntity.Id, updatedEntity.Key, updatedEntity.Value, updatedEntity.IsDev);

        // Update in repository
        var result = await configRepository.UpdateAsync(updatedEntity, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("Failed to update configuration {Id}: {Error}", id, result.Error);
            
            // Check if it's a duplicate key error
            if (result.Error!.Contains("already exists"))
            {
                return Results.Conflict(result.Error);
            }
            
            return Results.Problem(result.Error, statusCode: 500);
        }

        // Map entity to response
        var response = mapper.Map<ConfigResponse>(result.Value);

        return Results.Ok(response);
    }

    /// <summary>
    /// Soft deletes a configuration setting.
    /// </summary>
    private static async Task<IResult> DeleteConfigAsync(
        [FromRoute] int id,
        [FromServices] IConfigRepository configRepository,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing request to delete configuration: {Id}", id);

        var result = await configRepository.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("Failed to delete configuration {Id}: {Error}", id, result.Error);
            return Results.Problem(result.Error, statusCode: 500);
        }

        if (!result.Value)
        {
            return Results.NotFound($"Configuration with ID {id} not found");
        }

        return Results.NoContent();
    }
}