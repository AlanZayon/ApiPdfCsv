using System.Security.Claims;

namespace ApiPdfCsv.Shared.Helpers;

public static class UserSessionHelper
{
    public static string GetUserId(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public static string ResolveSessionId(HttpContext context)
    {
        var userId = GetUserId(context.User);
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("Usuário não autenticado.");
        }

        var sessionId = context.Request.Headers["X-User-Session"].FirstOrDefault()
                     ?? context.Request.Query["sessionId"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var guid = Guid.NewGuid().ToString("N")[..8];
            return $"{userId}_{guid}_{timestamp}";
        }

        if (sessionId.Contains("..", StringComparison.Ordinal)
            || sessionId.Contains('/', StringComparison.Ordinal)
            || sessionId.Contains('\\', StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Identificador de sessão inválido.");
        }

        return sessionId;
    }

    /// <summary>
    /// Gera um identificador exclusivo de pasta por job de upload,
    /// evitando colisão de arquivos quando vários uploads correm em paralelo.
    /// </summary>
    public static string CreateJobSessionId()
        => Guid.NewGuid().ToString("N");
}
