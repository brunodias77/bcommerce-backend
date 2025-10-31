using CatalogService.Infrastructure.Data.Context;
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
  //    services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    /// <summary>
    /// Configura os Health Checks para infraestrutura
    /// </summary>
    private static void AddInfrastructureHealthChecks(IServiceCollection services)
    {
        services.AddHealthChecks();
    }

    // TODO: crie o metodo para adicionar os repositories
    private static void AddRepositories(IServiceCollection services)
    {
        // services.AddScoped<ICategoryRepository, CategoryRepository>();
        // services.AddScoped<IFavoriteProductRepository, FavoriteProductRepository>();
        // services.AddScoped<IInboxEventRepository, InboxEventRepository>();
        // services.AddScoped<IOutboxEventRepository, OutboxEventRepository>();
        // services.AddScoped<IProductImageRepository, ProductImageRepository>();
        // services.AddScoped<IProductRepository, ProductRepository>();
        // services.AddScoped<IProductReviewRepository, ProductReviewRepository>();
        // services.AddScoped<IReceivedEventRepository, ReceivedEventRepository>();
        // services.AddScoped<IReviewVoteRepository, ReviewVoteRepository>();
    }
    
    /// <summary>
    /// Configura os serviços da camada de infraestrutura
    /// </summary>
    private static void AddServices(IServiceCollection services)
    {
      //  services.AddScoped<IPasswordEncripter, Infrastructure.Services.BCrypt>();
    }
}