// Modules/Authentication/Infrastructure/Services/TokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using ApiPdfCsv.Modules.Authentication.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Text;


namespace ApiPdfCsv.Modules.Authentication.Infrastructure.Services;

public class TokenService
{
    private readonly UserManager<ApplicationUser> _userManager;

    private readonly IConfiguration _config;

    public TokenService(IConfiguration config, UserManager<ApplicationUser> userManager)
    {
        _config = config;
        _userManager = userManager;

    }

    public async Task<string> GenerateTokenAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email!),
                new(ClaimTypes.Name, user.FullName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key não configurada.")));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(3),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
