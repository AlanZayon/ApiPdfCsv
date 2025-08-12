namespace ApiPdfCsv.Modules.Authentication.Application.DTOs.Auth;

public class UserInfoResponse
{
    public string? Email { get; set; }
    public string FullName { get; set; } = null!;
    public List<string> Roles { get; set; } = new();
}