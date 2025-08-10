using FileCategorization_Api.Contracts;
using FileCategorization_Api.Contracts.Identity;

namespace FileCategorization_Api.Interfaces;

public interface IIdentityService
{
    Task<ApiResponse<string>> Signup(SignupModelDto model);
    Task<ApiResponse<string>> Login(LoginModelDto model);
    Task<ApiResponse<string>> RefreshToken(TokenModelDto model);
    Task<ApiResponse<string>> RevokeToken(string username);
}