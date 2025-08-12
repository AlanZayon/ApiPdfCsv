using ApiPdfCsv.Modules.Authentication.Application.DTOs.Auth;

namespace ApiPdfCsv.Modules.Authentication.Application.Services;

public interface IAuthService
{
    Task<RegisterResponse> Register(RegisterRequest request);
    Task<LoginResponse> Login(LoginRequest request);
    Task Logout();
    Task<UserInfoResponse> GetCurrentUser();
}