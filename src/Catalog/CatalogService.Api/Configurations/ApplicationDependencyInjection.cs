namespace CatalogService.Api.Configurations;

public static class ApplicationDependencyInjection
{
    public static void AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediator(configuration);
        services.AddApplicationServices(configuration);
        services.AddApplicationHealthChecks();
        services.AddControllers(); // Direct call to avoid recursion
        services.AddSwagger();
    }

    private static void AddMediator(this IServiceCollection services, IConfiguration configuration)
    {
     //   services.AddMediator(typeof(CreateProductCommandHandler).Assembly);
    }


    private static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // services.AddScoped<ILoggedUser, LoggedUser>();
        // services.AddHttpContextAccessor();
        // services.AddMemoryCache();
    }

    private static void AddApplicationHealthChecks(this IServiceCollection services)
    {
    //    services.AddHealthChecks()
    //        .AddCheck<CatalogHealthCheck>("catalog");
    }

    private static void AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }
}