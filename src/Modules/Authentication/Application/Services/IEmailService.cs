// Modules/Email/Application/Services/IEmailService.cs
namespace ApiPdfCsv.Modules.Authentication.Application.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string email, string userName, string resetUrl);
    }
}