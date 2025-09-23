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
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Security.Claims;



namespace ApiPdfCsv.Modules.Authentication.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;

    private readonly TokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;


    public AuthService(UserManager<ApplicationUser> userManager, TokenService tokenService, IHttpContextAccessor httpContextAccessor, AppDbContext context, IEmailService emailService, IConfiguration configuration)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
        _dbContext = context;
        _emailService = emailService;
        _configuration = configuration;
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
                    var impostosUsuario = await _dbContext.Imposto
                        .Where(i => i.UserId == userId)
                        .ToListAsync();

                    _dbContext.Imposto.RemoveRange(impostosUsuario);

                    var codigosContaUsuario = await _dbContext.CodigoConta
                        .Where(c => c.UserId == userId)
                        .ToListAsync();

                    _dbContext.CodigoConta.RemoveRange(codigosContaUsuario);

                    var termoEspecial = await _dbContext.TermoEspecial
                        .Where(t => t.UserId == userId)
                        .ToListAsync();

                    _dbContext.TermoEspecial.RemoveRange(termoEspecial);

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
    public async Task<Result<bool>> ChangeUserName(ChangeUserNameRequest request)
    {
        try
        {
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

            if (string.IsNullOrWhiteSpace(request.NewFullName))
            {
                return Result<bool>.Failure("Nome inválido", new List<ValidationError>
            {
                new ValidationError("InvalidName", "O nome não pode estar vazio")
            });
            }

            if (user.FullName == request.NewFullName)
            {
                return Result<bool>.Failure("O novo nome deve ser diferente do atual", new List<ValidationError>
            {
                new ValidationError("SameName", "O novo nome não pode ser igual ao nome atual")
            });
            }

            // Atualizar o nome do usuário
            user.FullName = request.NewFullName;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(e => new ValidationError(e.Code, e.Description)).ToList();
                return Result<bool>.Failure("Falha ao alterar o nome", errors);
            }

            return Result<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Error("Erro inesperado ao tentar alterar o nome", ex);
        }
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

    public async Task<Result<bool>> ForgotPassword(ForgotPasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                return Result<bool>.SuccessResult(true);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var baseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
            var resetUrl = $"{baseUrl}/reset-password?email={user.Email}&token={encodedToken}";
            if (string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.FullName))
            {
                return Result<bool>.Failure("Dados do usuário incompletos para envio de email", new List<ValidationError>
                {
                    new ValidationError("MissingUserData", "Email ou nome do usuário está ausente")
                });
            }

            await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetUrl);

            return Result<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Error("Erro inesperado ao processar solicitação de redefinição de senha", ex);
        }
    }

    public async Task<Result<bool>> ResetPassword(ResetPasswordRequest request)
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

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Result<bool>.Failure("Operação de redefinição de senha falhou", new List<ValidationError>
                {
                    new ValidationError("InvalidRequest", "Solicitação inválida ou expirada")
                });
            }

            string decodedToken;
            try
            {
                var tokenBytes = WebEncoders.Base64UrlDecode(request.Token);
                decodedToken = Encoding.UTF8.GetString(tokenBytes);
            }
            catch
            {
                decodedToken = request.Token;
            }

            var passwordValidator = new PasswordValidator<ApplicationUser>();
            var validationResult = await passwordValidator.ValidateAsync(_userManager, user, request.NewPassword);

            if (!validationResult.Succeeded)
            {
                var errors = validationResult.Errors.Select(e => new ValidationError(e.Code, e.Description)).ToList();
                return Result<bool>.Failure("A nova senha não atende aos requisitos de segurança", errors);
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new ValidationError(e.Code, e.Description)).ToList();

                if (errors.Any(e => e.Code.Contains("InvalidToken")))
                {
                    return Result<bool>.Failure("Token inválido ou expirado", new List<ValidationError>
                    {
                        new ValidationError("InvalidToken", "O link de redefinição é inválido ou expirou")
                    });
                }

                return Result<bool>.Failure("Falha ao redefinir a senha", errors);
            }

            return Result<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Error("Erro inesperado ao redefinir senha", ex);
        }
    }

}