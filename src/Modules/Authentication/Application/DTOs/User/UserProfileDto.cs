// Modules/Authentication/Application/DTOs/User/UserProfileDto.cs
namespace ApiPdfCsv.Modules.Authentication.Application.DTOs.User
{
    public class UserProfileDto
    {
        public required string Id { get; set; }
        public required string Email { get; set; }
        public required string FullName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}