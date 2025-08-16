using FileCategorization_Shared.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using FileCategorization_Api.Infrastructure.Data;
using FileCategorization_Api.Domain.Entities;
using FileCategorization_Api.Domain.Entities.Identity;
using FileCategorization_Api.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FileCategorization_Api.Services;

public class IdentityService : IIdentityService
{
    private readonly ApplicationContext _context; // Database context
    private readonly ILogger<IdentityService> _logger; // Logger for logging information and errors
    private readonly IUtilityServices _utilityServices; // Utility services for encryption and other utilities
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenService _tokenService;

    public IdentityService(
        ApplicationContext context,
        ILogger<IdentityService> logger,
        IUtilityServices utilityServices,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager, ITokenService tokenService)
    {
        _context = context;
        _logger = logger;
        _utilityServices = utilityServices;
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
    }

    public async Task<ApiResponse<string>> Signup(SignupModelDto model)
    {
        try
        {
            var existingUser = await _userManager.FindByNameAsync(model.Email);
            if (existingUser != null)
            {
                return new ApiResponse<string>("User already exists", "BadRequest");
            }

            // Create a User role if it doesn't exist
            if ((await _roleManager.RoleExistsAsync(Roles.User)) == false)
            {
                var roleResult = await _roleManager
                    .CreateAsync(new IdentityRole(Roles.User));

                if (roleResult.Succeeded == false)
                {
                    var roleErros = roleResult.Errors.Select(e => e.Description);
                    _logger.LogError($"Failed to create user role. Errors : {string.Join(",", roleErros)}");
                    return new ApiResponse<string>(
                        $"Failed to create user role. Errors : {string.Join(",", roleErros)}", "BadRequest");
                }
            }

            ApplicationUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Email,
                Name = model.Name,
                EmailConfirmed = true
            };

            // Attempt to create a user
            var createUserResult = await _userManager.CreateAsync(user, model.Password);

            // Validate user creation. If a user is not created, log the error and
            // return the BadRequest along with the errors
            if (createUserResult.Succeeded == false)
            {
                var errors = createUserResult.Errors.Select(e => e.Description);
                _logger.LogError(
                    $"Failed to create user. Errors: {string.Join(", ", errors)}"
                );
                return new ApiResponse<string>($"Failed to create user. Errors: {string.Join(", ", errors)}",
                    "BadRequest");
            }

            // adding a role to user
            var addUserToRoleResult = await _userManager.AddToRoleAsync(user: user, role: Roles.User);

            if (addUserToRoleResult.Succeeded == false)
            {
                var errors = addUserToRoleResult.Errors.Select(e => e.Description);
                _logger.LogError($"Failed to add role to the user. Errors : {string.Join(",", errors)}");
            }

            return new ApiResponse<string>("User created", "Ok");
        }
        catch (Exception ex)
        {
            return new ApiResponse<string>(ex.Message, "BadRequest");
        }
    }

    public async Task<ApiResponse<string>> Login(LoginModelDto model)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                return new ApiResponse<string>("User with this username is not registered with us.", "BadRequest");
     
            }

            bool isValidPassword = await _userManager.CheckPasswordAsync(user, model.Password);
            if (isValidPassword == false)
            {
                return new ApiResponse<string>("Invalid password", "Unauthorized");
  
            }

            // creating the necessary claims
            List<Claim> authClaims =
            [
                new(ClaimTypes.Name, user.UserName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                // unique id for token
            ];

            var userRoles = await _userManager.GetRolesAsync(user);

            // Adding roles to the claims. So that we can get the user role from the token.
            authClaims.AddRange(userRoles.Select(userRole => new Claim(ClaimTypes.Role, userRole)));

            // generating access token
            var token = _tokenService.GenerateAccessToken(authClaims);

            string refreshToken = _tokenService.GenerateRefreshToken();

            //save refreshToken with exp date in the database
            var tokenInfo = _context.TokenInfo.FirstOrDefault(a => a.Username == user.UserName);

            // If tokenInfo is null for the user, create a new one
            if (tokenInfo == null)
            {
                var ti = new TokenInfo
                {
                    Username = user.UserName,
                    RefreshToken = refreshToken,
                    ExpiredAt = DateTime.UtcNow.AddDays(7)
                };
                _context.TokenInfo.Add(ti);
            }
            // Else, update the refresh token and expiration
            else
            {
                tokenInfo.RefreshToken = refreshToken;
                tokenInfo.ExpiredAt = DateTime.UtcNow.AddDays(7);
            }

            await _context.SaveChangesAsync();
            
            // Return the token and refresh token to the client
            var tokenResult = new TokenModelDto
            {
                AccessToken = token,
                RefreshToken = refreshToken
            };
            var tokenResultJson = JsonSerializer.Serialize(tokenResult);
            return new ApiResponse<string>(tokenResultJson, "Ok");
            

        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return new ApiResponse<string>(ex.Message, "Unauthorized");
        }
    }

    public async Task<ApiResponse<string>> RefreshToken(TokenModelDto model)
    {
        try
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(model.AccessToken);
            var username = principal.Identity.Name;

            var tokenInfo = _context.TokenInfo.SingleOrDefault(u => u.Username == username);

            if (tokenInfo == null || tokenInfo.RefreshToken != model.RefreshToken || tokenInfo.ExpiredAt <= DateTime.UtcNow)
            {
                return new ApiResponse<string>("Invalid refresh token. Please login again.", "BadRequest");
            }

            var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            tokenInfo.RefreshToken = newRefreshToken; // rotating the refresh token
            await _context.SaveChangesAsync();

            return new ApiResponse<string>(JsonSerializer.Serialize(new TokenModelDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            }), "Ok");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return new ApiResponse<string>(ex.Message, "BadRequest");
        }
    }

    public async Task<ApiResponse<string>> RevokeToken(string username)
    {
        try
        {
            var user = _context.TokenInfo.SingleOrDefault(u => u.Username == username);
            if (user == null)
            {
                return new  ApiResponse<string>("User with this username is not registered with us.", "BadRequest");
            }

            user.RefreshToken = string.Empty;
            await _context.SaveChangesAsync();

            return new ApiResponse<string>("User revoked", "Ok");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return new ApiResponse<string>(ex.Message, "Unauthorized");
        }
    }
}