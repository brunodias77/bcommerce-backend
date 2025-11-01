using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Validations;
using BuildingBlocks.Core.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Net;

namespace BuildingBlocks.Core.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment env)
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
        _logger.LogError(exception, "🚨 Exceção ocorreu: {ExceptionType} - {Message}", 
            exception.GetType().Name, exception.Message);

        context.Response.ContentType = "application/json";

        ApiResponse<object> response;
        HttpStatusCode statusCode;

        switch (exception)
        {
            case ValidationException validationEx:
                _logger.LogWarning("⚠️ Erro de validação: {Errors}", 
                    string.Join(", ", validationEx.Errors.Select(e => e.Message)));
                statusCode = HttpStatusCode.BadRequest;
                response = ApiResponse<object>.Fail(validationEx.Errors.ToList());
                break;

            case ArgumentException argumentEx:
                _logger.LogWarning("⚠️ Erro de argumento: {Message}", argumentEx.Message);
                statusCode = HttpStatusCode.BadRequest;
                response = ApiResponse<object>.Fail(argumentEx.Message);
                break;

            case DomainException domainEx:
                _logger.LogWarning("⚠️ Erro de domínio: {Message}", domainEx.Message);
                statusCode = HttpStatusCode.BadRequest;
                response = ApiResponse<object>.Fail(domainEx.Message);
                break;

            case KeyNotFoundException notFoundEx:
                _logger.LogWarning("🔍 Recurso não encontrado: {Message}", notFoundEx.Message);
                statusCode = HttpStatusCode.NotFound;
                response = ApiResponse<object>.Fail(notFoundEx.Message);
                break;

            case DbUpdateException dbEx:
                response = CreateDatabaseErrorResponse(dbEx);
                statusCode = HttpStatusCode.Conflict;
                break;

            case UnauthorizedAccessException unauthorizedEx:
                _logger.LogWarning("🔒 Acesso não autorizado: {Message}", unauthorizedEx.Message);
                statusCode = HttpStatusCode.Unauthorized;
                response = ApiResponse<object>.Fail("Acesso não autorizado");
                break;

            case InvalidOperationException invalidOpEx:
                _logger.LogWarning("⚠️ Operação inválida: {Message}", invalidOpEx.Message);
                statusCode = HttpStatusCode.BadRequest;
                response = ApiResponse<object>.Fail(invalidOpEx.Message);
                break;

            default:
                response = CreateGenericErrorResponse(exception);
                statusCode = HttpStatusCode.InternalServerError;
                break;
        }

        context.Response.StatusCode = (int)statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _env.IsDevelopment()
        };

        var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    private ApiResponse<object> CreateDatabaseErrorResponse(DbUpdateException ex)
    {
        _logger.LogError(ex, "💾 Erro de banco de dados ocorreu");

        // Verifica se é uma violação de constraint (chave duplicada, etc.)
        var innerException = ex.InnerException?.Message ?? ex.Message;
        
        if (innerException.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
            innerException.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
            innerException.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("🔄 Violação de constraint de chave duplicada: {Message}", innerException);
            
            var message = _env.IsDevelopment()
                ? $"Violação de constraint: {innerException}"
                : "Já existe um registro com essas informações";
                
            return ApiResponse<object>.Fail(message);
        }

        if (innerException.Contains("foreign key", StringComparison.OrdinalIgnoreCase) ||
            innerException.Contains("reference", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("🔗 Violação de constraint de chave estrangeira: {Message}", innerException);
            
            var message = _env.IsDevelopment()
                ? $"Violação de chave estrangeira: {innerException}"
                : "Não é possível realizar esta operação devido a dependências existentes";
                
            return ApiResponse<object>.Fail(message);
        }

        if (innerException.Contains("check constraint", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("✅ Violação de constraint de verificação: {Message}", innerException);
            
            var message = _env.IsDevelopment()
                ? $"Violação de constraint de verificação: {innerException}"
                : "Os dados fornecidos não atendem aos critérios de validação";
                
            return ApiResponse<object>.Fail(message);
        }

        // Erro genérico de banco de dados
        _logger.LogError("💥 Erro genérico de banco de dados: {Message}", innerException);
        
        var genericMessage = _env.IsDevelopment()
            ? $"Erro de banco de dados: {innerException}"
            : "Erro interno do servidor relacionado ao banco de dados";
            
        return ApiResponse<object>.Fail(genericMessage);
    }

    private ApiResponse<object> CreateGenericErrorResponse(Exception ex)
    {
        _logger.LogCritical(ex, "💥 Exceção crítica não tratada");
        
        var message = _env.IsDevelopment()
            ? $"Erro interno do servidor: {ex.Message}"
            : "Ocorreu um erro inesperado";
            
        return ApiResponse<object>.Fail(message);
    }
}

// Extension method para registrar
public static class GlobalExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}