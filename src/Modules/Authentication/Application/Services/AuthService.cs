// Modules/Authentication/Application/Services/AuthService.cs
using ApiPdfCsv.Modules.Authentication.Application.DTOs.Auth;
using ApiPdfCsv.Modules.Authentication.Application.DTOs.User;
using ApiPdfCsv.Modules.Authentication.Domain.Entities;
using ApiPdfCsv.Modules.Authentication.Infrastructure.Services;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.CrossCutting.Data;
using ApiPdfCsv.Shared.Results;
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

    public async Task<Result<RegisterResponse>> Register(RegisterRequest request)
    {
        try
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
                var errors = result.Errors.Select(e => new ValidationError(e.Code, e.Description)).ToList();

                return Result<RegisterResponse>.Failure("Falha ao registrar usuário", errors);
            }


            var codigosPadrao = await _dbContext.CodigoConta
                .Where(c => c.UserId == null)
                .ToListAsync();


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


            foreach (var imposto in impostosPadrao)
            {
                var codigoDebitoUsuario = codigosUsuario
                    .FirstOrDefault(c => c.Nome == imposto.Nome && c.Tipo == "debito");

                var codigoCreditoUsuario = codigosUsuario
                    .FirstOrDefault(c => c.Nome == imposto.Nome && c.Tipo == "credito");


                if (codigoDebitoUsuario == null || codigoCreditoUsuario == null)
                {
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


            await _dbContext.SaveChangesAsync();

            return Result<RegisterResponse>.SuccessResult(new RegisterResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName
            });
        }
        catch (Exception ex)
        {
            return Result<RegisterResponse>.Error("Erro inesperado ao registrar usuário", ex);
        }

    }


    public async Task<Result<LoginResponse>> Login(LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                return Result<LoginResponse>.Failure("Credenciais inválidas", new List<ValidationError>
            {
                new ValidationError ( "EmailNotFound", "E-mail não encontrado" )
            });
            }

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return Result<LoginResponse>.Failure("Credenciais inválidas", new List<ValidationError>
            {
                new ValidationError ( "InvalidPassword", "Senha incorreta")
            });
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

            return Result<LoginResponse>.SuccessResult(new LoginResponse
            {
                Email = user.Email!,
                FullName = user.FullName,
                Expiration = DateTime.UtcNow.AddHours(3),
                Roles = roles.ToList()
            });
        }
        catch (Exception ex)
        {
            return Result<LoginResponse>.Error("Erro inesperado ao tentar fazer login", ex);
        }
    }

    public Task Logout()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(-1)
        };

        _httpContextAccessor.HttpContext?.Response.Cookies.Delete("auth_token", cookieOptions);
        return Task.CompletedTask;
    }

    public async Task<Result<bool>> DeleteUser(string userId)
    {
        try
        {
            // Verificar se o usuário existe
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<bool>.Failure("Usuário não encontrado", new List<ValidationError>
            {
                new ValidationError("UserNotFound", "O usuário especificado não foi encontrado")
            });
            }

            // Obter a estratégia de execução
            var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                // Usar transação explícita dentro da estratégia de execução
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    // Remover todos os impostos vinculados ao usuário
                    var impostosUsuario = await _dbContext.Imposto
                        .Where(i => i.UserId == userId)
                        .ToListAsync();

                    _dbContext.Imposto.RemoveRange(impostosUsuario);

                    // Remover todos os códigos de conta vinculados ao usuário
                    var codigosContaUsuario = await _dbContext.CodigoConta
                        .Where(c => c.UserId == userId)
                        .ToListAsync();

                    _dbContext.CodigoConta.RemoveRange(codigosContaUsuario);

                    // Salvar alterações antes de remover o usuário
                    await _dbContext.SaveChangesAsync();

                    // Remover o usuário
                    var result = await _userManager.DeleteAsync(user);

                    if (!result.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        var errors = result.Errors.Select(e => new ValidationError(e.Code, e.Description)).ToList();
                        return Result<bool>.Failure("Falha ao excluir usuário", errors);
                    }

                    await transaction.CommitAsync();
                    return Result<bool>.SuccessResult(true);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Result<bool>.Error("Erro durante a exclusão dos dados do usuário", ex);
                }
            });
        }
        catch (Exception ex)
        {
            return Result<bool>.Error("Erro inesperado ao tentar excluir usuário", ex);
        }
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

    public async Task<Result<bool>> ChangePassword(ChangePasswordRequest request)
    {
        try
        {
            if (request.NewPassword != request.ConfirmPassword)
            {
                return Result<bool>.Failure("As senhas não coincidem", new List<ValidationError>
            {
                new ValidationError("PasswordMismatch", "A nova senha e a confirmação não coincidem")
            });
            }

            var passwordValidator = new PasswordValidator<ApplicationUser>();

            var principalForValidation = _httpContextAccessor.HttpContext?.User;
            var userForValidation = principalForValidation != null ? await _userManager.GetUserAsync(principalForValidation) : null;

            if (userForValidation == null)
            {
                return Result<bool>.Failure("Usuário não autenticado para validação da senha", new List<ValidationError>
                {
                    new ValidationError("NotAuthenticated", "Usuário não está autenticado para validação da senha")
                });
            }

            var validationResult = await passwordValidator.ValidateAsync(_userManager, userForValidation, request.NewPassword);

            if (!validationResult.Succeeded)
            {
                var errors = validationResult.Errors.Select(e => new ValidationError(e.Code, e.Description)).ToList();
                return Result<bool>.Failure("A nova senha não atende aos requisitos de segurança", errors);
            }

            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal == null)
            {
                return Result<bool>.Failure("Usuário não autenticado", new List<ValidationError>
            {
                new ValidationError("NotAuthenticated", "Usuário não está autenticado")
            });
            }

            var user = await _userManager.GetUserAsync(principal);
            if (user == null)
            {
                return Result<bool>.Failure("Usuário não encontrado", new List<ValidationError>
            {
                new ValidationError("UserNotFound", "Usuário não encontrado")
            });
            }

            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                return Result<bool>.Failure("Senha atual incorreta", new List<ValidationError>
            {
                new ValidationError("InvalidCurrentPassword", "A senha atual fornecida está incorreta")
            });
            }

            if (request.CurrentPassword == request.NewPassword)
            {
                return Result<bool>.Failure("A nova senha deve ser diferente da atual", new List<ValidationError>
            {
                new ValidationError("SamePassword", "A nova senha não pode ser igual à senha atual")
            });
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(
                user,
                request.CurrentPassword,
                request.NewPassword
            );

            if (!changePasswordResult.Succeeded)
            {
                var errors = changePasswordResult.Errors.Select(e => new ValidationError(e.Code, e.Description)).ToList();
                return Result<bool>.Failure("Falha ao alterar a senha", errors);
            }

            return Result<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Error("Erro inesperado ao tentar alterar a senha", ex);
        }
    }

}