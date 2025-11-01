

using CatalogService.Api.Configurations;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));



builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Configuração de CORS para aplicações frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendApps", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:3000") // Angular e React
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});



// Configuração da camada de infraestrutura
builder.Services.AddInfrastructure(builder.Configuration);

// Configuração da camada de aplicação
builder.Services.AddApplication(builder.Configuration);

var app = builder.Build();


// Log de inicialização
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 Catalog Service iniciado com sucesso!");
logger.LogInformation("🌍 Ambiente: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("📍 Diretório de trabalho: {WorkingDirectory}", Directory.GetCurrentDirectory());



// Configure the HTTP request pipeline.
// 1. Serilog Request Logging - DEVE VIR PRIMEIRO para capturar todos os requests
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "📥 HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) => ex != null
        ? Serilog.Events.LogEventLevel.Error
        : httpContext.Response.StatusCode > 499
            ? Serilog.Events.LogEventLevel.Error
            : Serilog.Events.LogEventLevel.Information;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown");
        diagnosticContext.Set("ServiceName", "Catalog.Api");
    };
});


// 2. CORS - DEVE VIR ANTES de outros middlewares
app.UseCors("AllowFrontendApps");

// 3. HTTPS Redirection - SEMPRE PRIMEIRO para forçar HTTPS
app.UseHttpsRedirection();

// 4. Swagger - Documentação da API
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 5. BuildingBlocks Middlewares - segurança, validação, monitoramento
//app.UseBuildingBlocksMiddleware(app.Environment.IsDevelopment());

// 6. Middleware de autenticação e autorização - APÓS os middlewares de segurança
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


logger.LogInformation("🎯 Catalog Service configurado e pronto para receber requisições!");

// Configurar evento para logar as URLs reais após a aplicação iniciar
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(() =>
{
    var server = app.Services.GetRequiredService<IServer>();
    var addressFeature = server.Features.Get<IServerAddressesFeature>();
    
    if (addressFeature?.Addresses?.Any() == true)
    {
        foreach (var address in addressFeature.Addresses)
        {
            logger.LogInformation("🌐 Catalog API rodando em: {Url}", address);
        }
    }
    else
    {
        logger.LogInformation("🌐 Catalog API iniciada (endereços não disponíveis)");
    }
});

try
{
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "💥 Erro crítico durante a execução do Catalog Service");
    throw;
}
finally
{
    Log.CloseAndFlush();
}