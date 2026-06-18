using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ApiPdfCsv.Shared.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Acesso negado"),
            InvalidDataException => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            FileNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor")
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode >= 500 ? "Ocorreu um erro inesperado." : exception.Message
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
