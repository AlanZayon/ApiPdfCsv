using Microsoft.AspNetCore.Identity;

namespace ApiPdfCsv.Modules.Authentication.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public required string FullName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}