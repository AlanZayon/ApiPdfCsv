namespace ApiPdfCsv.Modules.Authentication.Application.DTOs.Auth
{
    public class LoginResponse
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}