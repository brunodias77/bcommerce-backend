using BuildingBlocks.CQRS.Behaviors;
using BuildingBlocks.CQRS.Mediator;
using BuildingBlocks.CQRS.Validations;
using CatalogService.Api.Health;
using CatalogService.Application.Commands.Categories.CreateCategory;
using CatalogService.Application.Commands.Categories.DeleteCategory;
using CatalogService.Application.Commands.ProductImages.ReorderProductImages;
using CatalogService.Application.Commands.ProductImages.SetPrimaryProductImage;
using CatalogService.Application.Commands.Products.CreateProduct;
using CatalogService.Application.Commands.ProductReviews.CreateProductReview;
using CatalogService.Application.Commands.ProductReviews.DeleteProductReview;
using CatalogService.Application.Commands.ProductReviews.UpdateProductReview;

using CatalogService.Application.Commands.FavoriteProducts.AddProductToFavorites;

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
        // 1. Registrar Mediator
        services.AddMediator(typeof(CreateCategoryCommandHandler).Assembly);
    
        // 2. Registrar Behaviors (ordem importa!)
        // A execução será na ORDEM REVERSA do registro
    
    //    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));      
        // ↑ Executa PRIMEIRO (entrada e saída do pipeline)
    
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));   
        // ↑ Executa SEGUNDO (valida antes de abrir transação)
    
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));  
        // ↑ Executa TERCEIRO (envolve apenas o handler)
    }


    private static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Validators - registrar como IValidator<T> para o ValidationBehavior
        services.AddScoped<IValidator<CreateCategoryCommand>, CreateCategoryCommandValidator>();
        services.AddScoped<IValidator<DeleteCategoryCommand>, DeleteCategoryCommandValidator>();
        services.AddScoped<IValidator<CreateProductCommand>, CreateProductCommandValidator>();
        services.AddScoped<IValidator<ReorderProductImagesCommand>, ReorderProductImagesValidator>();
        services.AddScoped<IValidator<SetPrimaryProductImageCommand>, SetPrimaryProductImageValidator>();
        services.AddScoped<IValidator<CreateProductReviewCommand>, CreateProductReviewValidator>();
        services.AddScoped<IValidator<DeleteProductReviewCommand>, DeleteProductReviewValidator>();
        services.AddScoped<IValidator<UpdateProductReviewCommand>, UpdateProductReviewValidator>();

        services.AddScoped<IValidator<AddProductToFavoritesCommand>, AddProductToFavoritesCommandValidator>();
        
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