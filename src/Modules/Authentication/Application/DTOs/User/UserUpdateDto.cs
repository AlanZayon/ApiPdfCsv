// Modules/Authentication/Application/DTOs/User/UserUpdateDto.cs
namespace ApiPdfCsv.Modules.Authentication.Application.DTOs.User
{
    public class UserUpdateDto
    {
        public string? FullName { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}