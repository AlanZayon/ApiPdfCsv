// Modules/Email/Infrastructure/Services/EmailService.cs
using ApiPdfCsv.Modules.Authentication.Application.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace ApiPdfCsv.Modules.Authentication.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendPasswordResetEmailAsync(string email, string userName, string resetUrl)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            
            var portString = smtpSettings["Port"];
            if (string.IsNullOrWhiteSpace(portString))
                throw new InvalidOperationException("SMTP Port setting is missing or empty.");

            using var client = new SmtpClient(smtpSettings["Host"], int.Parse(portString))
            {
                Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                EnableSsl = true
            };

            var fromEmail = smtpSettings["FromEmail"];
            var fromName = smtpSettings["FromName"];

            if (string.IsNullOrWhiteSpace(fromEmail))
                throw new InvalidOperationException("SMTP FromEmail setting is missing or empty.");
            if (string.IsNullOrWhiteSpace(fromName))
                throw new InvalidOperationException("SMTP FromName setting is missing or empty.");

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = "Redefinição de Senha",
                Body = $@"
                <h3>Olá {userName},</h3>
                <p>Recebemos uma solicitação para redefinir sua senha.</p>
                <p>Clique no link abaixo para redefinir sua senha:</p>
                <p><a href='{resetUrl}'>{resetUrl}</a></p>
                <p>Se você não solicitou esta redefinição, ignore este email.</p>
                <p>Este link expirará em 24 horas.</p>",
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
        }
    }
}