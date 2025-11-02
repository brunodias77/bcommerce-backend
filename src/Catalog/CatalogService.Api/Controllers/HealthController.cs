using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Diagnostics;
using CatalogService.Infrastructure.Data.Context;

namespace CatalogService.Api.Controllers;

/// <summary>
/// Controller responsável pelos endpoints de health check do serviço de catálogo
/// </summary>
[ApiController]
[Route("health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly CatalogDbContext _dbContext;

    public HealthController(
        ILogger<HealthController> logger, 
        IWebHostEnvironment environment,
        CatalogDbContext dbContext)
    {
        _logger = logger;
        _environment = environment;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Endpoint básico de health check que verifica se a aplicação está funcionando
    /// </summary>
    /// <returns>Status de saúde da aplicação</returns>
    /// <response code="200">Aplicação está saudável</response>
    /// <response code="503">Aplicação não está saudável</response>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var databaseStatus = await CheckDatabaseConnectionAsync();
            var isHealthy = databaseStatus.IsHealthy;

            var response = new HealthResponse
            {
                Status = isHealthy ? "Healthy" : "Unhealthy",
                Service = "Catalog.Api",
                Version = GetVersion(),
                Environment = _environment.EnvironmentName,
                Timestamp = DateTime.UtcNow,
                Uptime = GetUptime(),
                Checks = new Dictionary<string, object>
                {
                    { "api", "Healthy" },
                    { "memory", GetMemoryUsage() }
                },
                Dependencies = new Dictionary<string, object>
                {
                    { "database", databaseStatus }
                }
            };

            _logger.LogInformation("🔍 [HealthController] Verificação executada: {Status}", response.Status);
            
            var statusCode = isHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [HealthController] Erro durante verificação");
            
            var errorResponse = new HealthResponse
            {
                Status = "Unhealthy",
                Service = "Catalog.Api",
                Version = GetVersion(),
                Environment = _environment.EnvironmentName,
                Timestamp = DateTime.UtcNow,
                Error = ex.Message,
                Dependencies = new Dictionary<string, object>
                {
                    { "database", new { status = "Error", message = ex.Message } }
                }
            };

            return StatusCode(StatusCodes.Status503ServiceUnavailable, errorResponse);
        }
    }

    /// <summary>
    /// Endpoint de readiness check que verifica se a aplicação está pronta para receber requests
    /// </summary>
    /// <returns>Status de prontidão da aplicação</returns>
    /// <response code="200">Aplicação está pronta</response>
    /// <response code="503">Aplicação não está pronta</response>
    [HttpGet("ready")]
    [ProducesResponseType(typeof(ReadinessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReadinessResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            var databaseStatus = await CheckDatabaseConnectionAsync();
            var isReady = databaseStatus.IsHealthy;

            var response = new ReadinessResponse
            {
                Status = isReady ? "Ready" : "Not Ready",
                Service = "Catalog.Api",
                Timestamp = DateTime.UtcNow,
                Checks = new Dictionary<string, object>
                {
                    { "configuration", "Ready" },
                    { "dependencies", isReady ? "Ready" : "Not Ready" },
                    { "database", databaseStatus }
                }
            };

            var statusCode = isReady ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
            
            _logger.LogInformation("🔍 [HealthController] Verificação de prontidão executada: {Status}", response.Status);
            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [HealthController] Erro durante verificação de prontidão");
            
            var errorResponse = new ReadinessResponse
            {
                Status = "Not Ready",
                Service = "Catalog.Api",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message,
                Checks = new Dictionary<string, object>
                {
                    { "database", new { status = "Error", message = ex.Message } }
                }
            };

            return StatusCode(StatusCodes.Status503ServiceUnavailable, errorResponse);
        }
    }

    /// <summary>
    /// Endpoint de liveness check que verifica se a aplicação está viva e funcionando
    /// </summary>
    /// <returns>Status de vida da aplicação</returns>
    /// <response code="200">Aplicação está viva</response>
    /// <response code="503">Aplicação não está respondendo</response>
    [HttpGet("live")]
    [ProducesResponseType(typeof(LivenessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LivenessResponse), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetLiveness()
    {
        try
        {
            var response = new LivenessResponse
            {
                Status = "Alive",
                Service = "Catalog.Api",
                Timestamp = DateTime.UtcNow,
                ProcessId = Environment.ProcessId,
                MachineName = Environment.MachineName,
                Uptime = GetUptime()
            };

            _logger.LogDebug("🔍 [HealthController] Verificação de vitalidade executada com sucesso");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [HealthController] Erro durante verificação de vitalidade");
            
            var errorResponse = new LivenessResponse
            {
                Status = "Dead",
                Service = "Catalog.Api",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            };

            return StatusCode(StatusCodes.Status503ServiceUnavailable, errorResponse);
        }
    }

    #region Private Methods

    /// <summary>
    /// Verifica a conectividade com o banco de dados
    /// </summary>
    /// <returns>Status da conexão com o banco</returns>
    private async Task<DatabaseStatus> CheckDatabaseConnectionAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Tenta executar uma query simples para verificar a conectividade
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
            
            stopwatch.Stop();
            
            return new DatabaseStatus
            {
                Status = "Connected",
                IsHealthy = true,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ConnectionString = _dbContext.Database.GetConnectionString()?.Split(';')[0] // Apenas o servidor, sem credenciais
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogWarning(ex, "⚠️ [HealthController] Falha na conexão com o banco de dados");
            
            return new DatabaseStatus
            {
                Status = "Disconnected",
                IsHealthy = false,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Error = ex.Message,
                ConnectionString = _dbContext.Database.GetConnectionString()?.Split(';')[0]
            };
        }
    }

    /// <summary>
    /// Obtém a versão da aplicação
    /// </summary>
    /// <returns>Versão da aplicação</returns>
    private string GetVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
    }

    /// <summary>
    /// Obtém o tempo de atividade da aplicação
    /// </summary>
    /// <returns>Tempo de atividade</returns>
    private TimeSpan GetUptime()
    {
        using var process = Process.GetCurrentProcess();
        return DateTime.Now - process.StartTime;
    }

    /// <summary>
    /// Obtém informações sobre o uso de memória
    /// </summary>
    /// <returns>Uso de memória em MB</returns>
    private string GetMemoryUsage()
    {
        var workingSet = Environment.WorkingSet;
        var memoryMB = workingSet / (1024 * 1024);
        return $"{memoryMB} MB";
    }

    #endregion
}

#region Response Models

/// <summary>
/// Modelo de resposta para health check básico
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Status geral da aplicação
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Nome do serviço
    /// </summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Versão da aplicação
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Ambiente de execução
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp da verificação
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Tempo de atividade da aplicação
    /// </summary>
    public TimeSpan? Uptime { get; set; }

    /// <summary>
    /// Verificações detalhadas
    /// </summary>
    public Dictionary<string, object>? Checks { get; set; }

    /// <summary>
    /// Status das dependências (banco de dados, cache, etc.)
    /// </summary>
    public Dictionary<string, object>? Dependencies { get; set; }

    /// <summary>
    /// Mensagem de erro (se houver)
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Modelo de resposta para readiness check
/// </summary>
public class ReadinessResponse
{
    /// <summary>
    /// Status de prontidão
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Nome do serviço
    /// </summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp da verificação
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Verificações de dependências
    /// </summary>
    public Dictionary<string, object>? Checks { get; set; }

    /// <summary>
    /// Mensagem de erro (se houver)
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Modelo de resposta para liveness check
/// </summary>
public class LivenessResponse
{
    /// <summary>
    /// Status de vida da aplicação
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Nome do serviço
    /// </summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp da verificação
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// ID do processo
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Nome da máquina
    /// </summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// Tempo de atividade
    /// </summary>
    public TimeSpan? Uptime { get; set; }

    /// <summary>
    /// Mensagem de erro (se houver)
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Status da conexão com o banco de dados
/// </summary>
public class DatabaseStatus
{
    /// <summary>
    /// Status da conexão (Connected/Disconnected)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Indica se a conexão está saudável
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Tempo de resposta em milissegundos
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// String de conexão (apenas servidor, sem credenciais)
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Mensagem de erro (se houver)
    /// </summary>
    public string? Error { get; set; }
}

#endregion