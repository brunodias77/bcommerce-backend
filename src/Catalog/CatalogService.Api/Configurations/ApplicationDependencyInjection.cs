using BuildingBlocks.CQRS.Behaviors;
using BuildingBlocks.CQRS.Mediator;
using BuildingBlocks.CQRS.Validations;
using CatalogService.Api.Health;
using CatalogService.Application.Commands.Categories.CreateCategory;
using CatalogService.Application.Commands.Categories.DeleteCategory;
using CatalogService.Application.Commands.Products.CreateProduct;

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
        // Registrar Mediator com assembly da Application
        services.AddMediator(typeof(CreateCategoryCommandHandler).Assembly);
        
        // Registrar ValidationBehavior manualmente (executa primeiro)
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        
        // Registrar TransactionBehavior manualmente (executa após ValidationBehavior)
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
    }


    private static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Validators - registrar como IValidator<T> para o ValidationBehavior
        services.AddScoped<IValidator<CreateCategoryCommand>, CreateCategoryCommandValidator>();
        services.AddScoped<IValidator<DeleteCategoryCommand>, DeleteCategoryCommandValidator>();
        services.AddScoped<IValidator<CreateProductCommand>, CreateProductCommandValidator>();
        
        // services.AddScoped<ILoggedUser, LoggedUser>();
        // services.AddHttpContextAccessor();
        // services.AddMemoryCache();
    }

    private static void AddApplicationHealthChecks(this IServiceCollection services)
    {
    services.AddHealthChecks()
        .AddCheck<CatalogHealthCheck>("catalog");
    }

    private static void AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }
}