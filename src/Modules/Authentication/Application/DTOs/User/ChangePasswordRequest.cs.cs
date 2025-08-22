// Modules/Authentication/Application/DTOs/Auth/ChangePasswordRequest.cs
namespace ApiPdfCsv.Modules.Authentication.Application.DTOs.User;

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}