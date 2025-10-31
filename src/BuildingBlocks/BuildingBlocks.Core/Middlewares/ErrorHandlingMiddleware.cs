using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using BuildingBlocks.Core.Exceptions;

namespace BuildingBlocks.Core.Middlewares;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Uma exceção não tratada ocorreu: {Message}", exception.Message);

        // Define o status code e a resposta
        HttpStatusCode statusCode;
        object response;

        var exceptionType = exception.GetType();

        if (exceptionType == typeof(DomainException))
        {
            // Erros de regras de negócio (Domínio)
            statusCode = HttpStatusCode.BadRequest; // 400
            response = new { error = exception.Message };
        }
        else if (exceptionType == typeof(KeyNotFoundException) || exceptionType == typeof(ArgumentNullException))
        {
            // Erros comuns que podem ser mapeados para 404 ou 400
            statusCode = HttpStatusCode.NotFound; // 404
            response = new { error = exception.Message };
        }
        else
        {
            // Erros inesperados do sistema
            statusCode = HttpStatusCode.InternalServerError; // 500
            
            // Em produção, não vaze detalhes do erro
            if (_env.IsDevelopment())
            {
                response = new { error = "Erro interno no servidor.", details = exception.ToString() };
            }
            else
            {
                response = new { error = "Ocorreu um erro inesperado. Tente novamente mais tarde." };
            }
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}