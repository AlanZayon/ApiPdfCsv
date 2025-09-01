// Modules/Authentication/Application/DTOs/Auth/ResetPasswordRequest.cs
namespace ApiPdfCsv.Modules.Authentication.Application.DTOs.Auth
{
    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}