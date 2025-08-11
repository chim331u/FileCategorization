using FileCategorization_Api.Interfaces;
using System.ComponentModel;

namespace FileCategorization_Api.Endpoints;

/// <summary>
/// Provides extension methods to map utility-related endpoints.
/// </summary>
public static class UtilitiesEndpoint
{
    /// <summary>
    /// Maps the utility-related endpoints to the specified <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">The <see cref="IEndpointRouteBuilder"/> to map the endpoints to.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> with the mapped endpoints.</returns>
    public static IEndpointRouteBuilder MapUtilitiesEndPoint(this IEndpointRouteBuilder app)
    {
        // Define the endpoints
            
        /// <summary>
        /// Endpoint to encrypt a string.
        /// </summary>
        /// <param name="plainText">The plain text to encrypt.</param>
        /// <param name="utilityServices">The service to perform the encryption.</param>
        app.MapPost("/EncryptString", async (string plainText, IUtilityServices utilityServices) =>
        {
            string cryptedtext = await utilityServices.EncryptString(plainText);
            return Results.Ok(cryptedtext);
        })
        .WithMetadata(new ObsoleteAttribute("This endpoint is deprecated. Use POST /api/v2/crypto/encrypt instead.", false));
        
        /// <summary>
        /// Endpoint to decrypt a string.
        /// </summary>
        /// <param name="cryptedText">The encrypted text to decrypt.</param>
        /// <param name="utilityServices">The service to perform the decryption.</param>
        app.MapPost("/DencryptString", async (string cryptedText, IUtilityServices utilityServices) =>
        {
            string decryptedtext = await utilityServices.DecryptString(cryptedText);
            return Results.Ok(decryptedtext);
        })
        .WithMetadata(new ObsoleteAttribute("This endpoint is deprecated. Use POST /api/v2/crypto/decrypt instead.", false));
        
        /// <summary>
        /// Endpoint to compute the SHA-256 hash of a string.
        /// </summary>
        /// <param name="text">The text to hash.</param>
        /// <param name="utilityServices">The service to compute the hash.</param>
        app.MapPost("/Hash256String", async (string text, IUtilityServices utilityServices) =>
        {
            string hashedtext = await utilityServices.HashString_SHA256(text);
            return Results.Ok(hashedtext);
        })
        .WithMetadata(new ObsoleteAttribute("This endpoint is deprecated. Use POST /api/v2/crypto/hash instead.", false));
        
        /// <summary>
        /// Endpoint to verify a string against an existing SHA-256 hash.
        /// </summary>
        /// <param name="text">The plain text to verify.</param>
        /// <param name="hashedText">The hash to compare against.</param>
        /// <param name="utilityServices">The service to perform the verification.</param>
        app.MapPost("/VerifyHash", async (string text, string hashedText, IUtilityServices utilityServices) =>
        {
            bool areEquals = await utilityServices.VerifyHash_SHA256(text, hashedText);
            return Results.Ok(areEquals);
        })
        .WithMetadata(new ObsoleteAttribute("This endpoint is deprecated. Use POST /api/v2/crypto/verify instead.", false));
        
        return app;
    }
}