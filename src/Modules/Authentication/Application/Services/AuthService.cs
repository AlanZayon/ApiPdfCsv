// Modules/Authentication/Application/Services/AuthService.cs
using ApiPdfCsv.Modules.Authentication.Application.DTOs.Auth;
using ApiPdfCsv.Modules.Authentication.Domain.Entities;
using ApiPdfCsv.Modules.Authentication.Infrastructure.Services;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.CrossCutting.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;



namespace ApiPdfCsv.Modules.Authentication.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;

    private readonly TokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;


    public AuthService(UserManager<ApplicationUser> userManager, TokenService tokenService, IHttpContextAccessor httpContextAccessor, AppDbContext context)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
        _dbContext = context;

    }

    public async Task<RegisterResponse> Register(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            throw new ApplicationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        Console.WriteLine($"Criando usuário: {user.Id}");

        var codigosPadrao = await _dbContext.CodigoConta
            .Where(c => c.UserId == null)
            .ToListAsync();

        Console.WriteLine($"Códigos padrão encontrados: {codigosPadrao.Count}");

        var codigosUsuario = new List<CodigoConta>();

        foreach (var codigo in codigosPadrao)
        {
            var codigoUsuario = new CodigoConta
            {
                Nome = codigo.Nome,
                Codigo = codigo.Codigo,
                Tipo = codigo.Tipo,
                UserId = user.Id
            };
            codigosUsuario.Add(codigoUsuario);
            _dbContext.CodigoConta.Add(codigoUsuario);
        }

        await _dbContext.SaveChangesAsync();

        var impostosPadrao = await _dbContext.Imposto
            .Where(i => i.UserId == null)
            .ToListAsync();

        Console.WriteLine($"Impostos padrão encontrados: {impostosPadrao.Count}");

        foreach (var imposto in impostosPadrao)
        {
            var codigoDebitoUsuario = codigosUsuario
                .FirstOrDefault(c => c.Nome == imposto.Nome && c.Tipo == "debito");

            var codigoCreditoUsuario = codigosUsuario
                .FirstOrDefault(c => c.Nome == imposto.Nome && c.Tipo == "credito");


            if (codigoDebitoUsuario == null || codigoCreditoUsuario == null)
            {
                Console.WriteLine($"Código de conta não encontrado para imposto: {imposto.Nome}");
                continue;
            }

            var impostoUsuario = new Imposto
            {
                Nome = imposto.Nome,
                UserId = user.Id,
                CodigoDebitoId = codigoDebitoUsuario?.Id,
                CodigoCreditoId = codigoCreditoUsuario?.Id
            };

            _dbContext.Imposto.Add(impostoUsuario);
        }

        Console.WriteLine($"Salvando usuário e dados padrão no banco de dados: {user.Id}");

        await _dbContext.SaveChangesAsync();

        return new RegisterResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName
        };
    }


    public async Task<LoginResponse> Login(LoginRequest request)
    {

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new ApplicationException("Invalid credentials");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user);


        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddHours(3)
        };

        _httpContextAccessor.HttpContext?.Response.Cookies.Append("auth_token", token, cookieOptions);

        return new LoginResponse
        {
            Email = user.Email!,
            FullName = user.FullName,
            Expiration = DateTime.UtcNow.AddHours(3),
            Roles = roles.ToList()
        };
    }

    public Task Logout()
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete("auth_token");
        return Task.CompletedTask;
    }

    public async Task<UserInfoResponse> GetCurrentUser()
    {

        var principal = _httpContextAccessor.HttpContext?.User;

        if (principal == null)
        {
            throw new ApplicationException("User not authenticated");
        }
        var user = await _userManager.GetUserAsync(principal);

        if (user == null)
        {
            throw new ApplicationException("User not authenticated");
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new UserInfoResponse
        {
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles.ToList()
        };
    }

}