using FileCategorization_Shared.Common;
using FileCategorization_Api.Domain.Entities;
using FileCategorization_Api.Domain.Entities.Identity;

namespace FileCategorization_Api.Interfaces;

public interface IIdentityService
{
    Task<ApiResponse<string>> Signup(SignupModelDto model);
    Task<ApiResponse<string>> Login(LoginModelDto model);
    Task<ApiResponse<string>> RefreshToken(TokenModelDto model);
    Task<ApiResponse<string>> RevokeToken(string username);
}