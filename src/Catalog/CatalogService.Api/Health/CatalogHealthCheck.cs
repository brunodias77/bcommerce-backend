using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CatalogService.Api.Health;

/// <summary>
/// Health check personalizado para o serviço de catálogo
/// </summary>
public class CatalogHealthCheck : IHealthCheck
{
    private readonly ILogger<CatalogHealthCheck> _logger;

    public CatalogHealthCheck(ILogger<CatalogHealthCheck> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executa a verificação de saúde do catálogo
    /// </summary>
    /// <param name="context">Contexto da verificação</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da verificação</returns>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Aqui você pode adicionar verificações específicas do catálogo
            // Por exemplo: verificar se o serviço pode acessar recursos críticos
            
            var data = new Dictionary<string, object>
            {
                { "service", "Catalog.Api" },
                { "status", "healthy" },
                { "timestamp", DateTime.UtcNow }
            };

            _logger.LogDebug("Catalog health check executado com sucesso");
            
            return Task.FromResult(HealthCheckResult.Healthy("Catalog service is healthy", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante catalog health check");
            
            return Task.FromResult(HealthCheckResult.Unhealthy("Catalog service is unhealthy", ex));
        }
    }
}