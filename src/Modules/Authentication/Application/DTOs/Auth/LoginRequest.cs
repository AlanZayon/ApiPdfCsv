using System.ComponentModel.DataAnnotations;

namespace ApiPdfCsv.Modules.Authentication.Application.DTOs.Auth
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "O e-mail não é válido.")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres.")]
        public required string Password { get; set; }
    }
}
