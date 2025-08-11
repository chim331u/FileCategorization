using FileCategorization_Api.Contracts.Utility;
using FileCategorization_Api.Core.Interfaces;
using FileCategorization_Api.Presentation.Filters;
using Microsoft.AspNetCore.Mvc;

namespace FileCategorization_Api.Presentation.Endpoints;

/// <summary>
/// Extension methods for mapping utility endpoints.
/// </summary>
public static class UtilityEndpoint
{
    /// <summary>
    /// Maps the utility endpoints to the specified route group.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <returns>The route group builder for chaining.</returns>
    public static RouteGroupBuilder MapUtilityEndPoints(this RouteGroupBuilder group)
    {
        // Encrypt string
        group.MapPost("/crypto/encrypt", EncryptStringAsync)
            .WithName("EncryptString_v2")
            .WithSummary("[v2] Encrypts a plain text string")
            .WithDescription("[v2 - Repository Pattern] Encrypts a plain text string using AES encryption with validation")
            .Produces<StringUtilityResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<EncryptStringRequest>>();

        // Decrypt string
        group.MapPost("/crypto/decrypt", DecryptStringAsync)
            .WithName("DecryptString_v2")
            .WithSummary("[v2] Decrypts an encrypted string")
            .WithDescription("[v2 - Repository Pattern] Decrypts an encrypted string using AES decryption with validation")
            .Produces<StringUtilityResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<DecryptStringRequest>>();

        // Hash string
        group.MapPost("/crypto/hash", HashStringAsync)
            .WithName("HashString_v2")
            .WithSummary("[v2] Generates SHA256 hash of text")
            .WithDescription("[v2 - Repository Pattern] Computes SHA256 hash of the provided text with validation")
            .Produces<StringUtilityResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<HashStringRequest>>();

        // Verify hash
        group.MapPost("/crypto/verify", VerifyHashAsync)
            .WithName("VerifyHash_v2")
            .WithSummary("[v2] Verifies text against SHA256 hash")
            .WithDescription("[v2 - Repository Pattern] Verifies if plain text matches the provided SHA256 hash with validation")
            .Produces<BooleanUtilityResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<VerifyHashRequest>>();

        return group;
    }

    /// <summary>
    /// Encrypts a plain text string.
    /// </summary>
    private static async Task<IResult> EncryptStringAsync(
        [FromBody] EncryptStringRequest request,
        [FromServices] IUtilityRepository utilityRepository,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing string encryption request");

        var result = await utilityRepository.EncryptStringAsync(request.PlainText, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("String encryption failed: {Error}", result.ErrorMessage);
            return Results.Problem(result.ErrorMessage, statusCode: 500);
        }

        var response = new StringUtilityResponse
        {
            Result = result.Data!,
            IsSuccess = true,
            Message = "String encrypted successfully"
        };

        return Results.Ok(response);
    }

    /// <summary>
    /// Decrypts an encrypted string.
    /// </summary>
    private static async Task<IResult> DecryptStringAsync(
        [FromBody] DecryptStringRequest request,
        [FromServices] IUtilityRepository utilityRepository,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing string decryption request");

        var result = await utilityRepository.DecryptStringAsync(request.EncryptedText, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("String decryption failed: {Error}", result.ErrorMessage);
            return Results.Problem(result.ErrorMessage, statusCode: 500);
        }

        var response = new StringUtilityResponse
        {
            Result = result.Data!,
            IsSuccess = true,
            Message = "String decrypted successfully"
        };

        return Results.Ok(response);
    }

    /// <summary>
    /// Computes SHA256 hash of text.
    /// </summary>
    private static async Task<IResult> HashStringAsync(
        [FromBody] HashStringRequest request,
        [FromServices] IUtilityRepository utilityRepository,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing string hashing request");

        var result = await utilityRepository.HashStringAsync(request.Text, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("String hashing failed: {Error}", result.ErrorMessage);
            return Results.Problem(result.ErrorMessage, statusCode: 500);
        }

        var response = new StringUtilityResponse
        {
            Result = result.Data!,
            IsSuccess = true,
            Message = "String hashed successfully"
        };

        return Results.Ok(response);
    }

    /// <summary>
    /// Verifies text against SHA256 hash.
    /// </summary>
    private static async Task<IResult> VerifyHashAsync(
        [FromBody] VerifyHashRequest request,
        [FromServices] IUtilityRepository utilityRepository,
        [FromServices] ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing hash verification request");

        var result = await utilityRepository.VerifyHashAsync(request.PlainText, request.Hash, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("Hash verification failed: {Error}", result.ErrorMessage);
            return Results.Problem(result.ErrorMessage, statusCode: 500);
        }

        var response = new BooleanUtilityResponse
        {
            Result = result.Data,
            IsSuccess = true,
            Message = result.Data ? "Hash verification successful - match found" : "Hash verification successful - no match"
        };

        return Results.Ok(response);
    }
}