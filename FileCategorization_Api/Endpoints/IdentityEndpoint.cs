using System.Text.Json;
using FileCategorization_Api.Domain.Entities.Identity;
using FileCategorization_Api.Interfaces;

namespace FileCategorization_Api.Endpoints;

public static class IdentityEndpoint
{
    public static IEndpointRouteBuilder MapIdentityEndPoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/Signup", async (SignupModelDto model, IIdentityService identityService) =>
        {
            var signupResult = await identityService.Signup(model);

            switch (signupResult.Message)
            {
                case "BadRequest":
                    return Results.BadRequest(signupResult.Data);
                case "Ok":
                    return Results.Ok(signupResult.Data);
            }

            return Results.BadRequest();
        });
        
        app.MapPost("/Login", async (LoginModelDto model, IIdentityService identityService) =>
        {
            var loginResult = await identityService.Login(model);

            switch (loginResult.Message)
            {
                case "BadRequest":
                    return Results.BadRequest(loginResult.Data);
                case "Unauthorized":
                    return Results.Unauthorized();
                case "Ok":
                    return Results.Ok(JsonSerializer.Deserialize<TokenModelDto>(loginResult.Data));
            }

            return Results.BadRequest();
        });
        
        app.MapPost("/RefreshToken", async (TokenModelDto model, IIdentityService identityService) =>
        {
            var refreshResult = await identityService.RefreshToken(model);

            switch (refreshResult.Message)
            {
                case "BadRequest":
                    return Results.BadRequest(refreshResult.Data);
                case "Unauthorized":
                    return Results.Unauthorized();
                case "Ok":
                    return Results.Ok(JsonSerializer.Deserialize<TokenModelDto>(refreshResult.Data));
            }

            return Results.BadRequest();
        });
        
        app.MapPost("/RevokeToken", async (string username, IIdentityService identityService) =>
        {
            var revokeResult = await identityService.RevokeToken(username);

            switch (revokeResult.Message)
            {
                case "BadRequest":
                    return Results.BadRequest(revokeResult.Data);
                case "Unauthorized":
                    return Results.Unauthorized();
                case "Ok":
                    return Results.Ok();
            }

            return Results.BadRequest();
        }).RequireAuthorization();
        return app;
    }    
}