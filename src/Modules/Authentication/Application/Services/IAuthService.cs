using ApiPdfCsv.Modules.Authentication.Application.DTOs.Auth;
using ApiPdfCsv.Modules.Authentication.Application.DTOs.User;
using ApiPdfCsv.Shared.Results;

namespace ApiPdfCsv.Modules.Authentication.Application.Services;

public interface IAuthService
{
    Task<Result<RegisterResponse>> Register(RegisterRequest request);
    Task<Result<LoginResponse>> Login(LoginRequest request);
    Task Logout();
    Task<UserInfoResponse> GetCurrentUser();
    Task<Result<bool>> DeleteUser(string userId);
    Task<Result<bool>> ChangeUserName(ChangeUserNameRequest request);
    Task<Result<bool>> ChangePassword(ChangePasswordRequest request);
    Task<Result<bool>> ForgotPassword(ForgotPasswordRequest request);
    Task<Result<bool>> ResetPassword(ResetPasswordRequest request);
}