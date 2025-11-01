using BuildingBlocks.Core.Data;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using CatalogService.Infrastructure.Data.Repositories;
using CatalogService.Infrastructure.Data.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Api.Configurations;

public static class InfraDependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabase(services, configuration);
        AddUnitOfWork(services);
        AddInfrastructureHealthChecks(services);
        AddRepositories(services);
        AddServices(services);
    }

    /// <summary>
    /// Configura o Entity Framework com PostgreSQL
    /// </summary>
    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
    }
    
    /// <summary>
    /// Configura o UnitOfWork
    /// </summary>
    private static void AddUnitOfWork(IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    /// <summary>
    /// Configura os Health Checks para infraestrutura
    /// </summary>
    private static void AddInfrastructureHealthChecks(IServiceCollection services)
    {
      //  services.AddHealthChecks();
    }

    /// <summary>
    /// Configura os repositories da camada de infraestrutura
    /// </summary>
    private static void AddRepositories(IServiceCollection services)
    {
        // Repositories para Aggregates
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductReviewRepository, ProductReviewRepository>();
        
        // Repositories para Entities
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<IFavoriteProductRepository, FavoriteProductRepository>();
        services.AddScoped<IReviewVoteRepository, ReviewVoteRepository>();
        
        // Repositories para Events
        services.AddScoped<IOutboxEventRepository, OutboxEventRepository>();
        services.AddScoped<IInboxEventRepository, InboxEventRepository>();
        services.AddScoped<IReceivedEventRepository, ReceivedEventRepository>();
    }
    
    /// <summary>
    /// Configura os serviços da camada de infraestrutura
    /// </summary>
    private static void AddServices(IServiceCollection services)
    {
      //  services.AddScoped<IPasswordEncripter, Infrastructure.Services.BCrypt>();
    }
}