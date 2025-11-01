Analisando seu projeto, vejo que você já está no caminho certo usando ApiResponse para padronizar retornos. Vou propor uma abordagem híbrida que aproveita o melhor dos dois mundos: exceptions para erros de domínio e ApiResponse para coordenar respostas.
🎯 Estratégia Recomendada
1. Exceções de Domínio (Domain Layer)
Mantenha exceções no domínio para violações de regras de negócio:
csharp// Domain/Exceptions/CatalogDomainException.cs
public abstract class CatalogDomainException : Exception
{
    public string Code { get; }
    
    protected CatalogDomainException(string code, string message) 
        : base(message)
    {
        Code = code;
    }
}

// Exceções específicas
public class ProductNotFoundException : CatalogDomainException
{
    public ProductNotFoundException(Guid productId) 
        : base("PRODUCT_NOT_FOUND", $"Product with ID {productId} not found")
    {
    }
}

public class DuplicateSlugException : CatalogDomainException
{
    public DuplicateSlugException(string slug) 
        : base("DUPLICATE_SLUG", $"Slug '{slug}' already exists")
    {
    }
}

public class InsufficientStockException : CatalogDomainException
{
    public InsufficientStockException(Guid productId, int requested, int available) 
        : base("INSUFFICIENT_STOCK", 
              $"Insufficient stock for product {productId}. Requested: {requested}, Available: {available}")
    {
    }
}
2. Global Exception Handler (API Layer)
Centralize o tratamento de exceções:
csharp// Api/Middlewares/GlobalExceptionHandlerMiddleware.cs
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
        _logger.LogError(exception, "❌ Unhandled exception: {Message}", exception.Message);

        var (statusCode, response) = exception switch
        {
            // Domain Exceptions
            CatalogDomainException domainEx => (
                StatusCodes.Status400BadRequest,
                ApiResponse<object>.Fail(new List<Error> 
                { 
                    new Error(domainEx.Message) 
                })
            ),
            
            ProductNotFoundException => (
                StatusCodes.Status404NotFound,
                ApiResponse<object>.Fail("Product not found")
            ),
            
            DuplicateSlugException => (
                StatusCodes.Status409Conflict,
                ApiResponse<object>.Fail("Slug already exists")
            ),
            
            // Validation Exceptions (from BuildingBlocks)
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                ApiResponse<object>.Fail(validationEx.Errors.ToList())
            ),
            
            // Database Exceptions
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                ApiResponse<object>.Fail("The record was modified by another user")
            ),
            
            DbUpdateException dbEx => (
                StatusCodes.Status500InternalServerError,
                CreateDatabaseErrorResponse(dbEx)
            ),
            
            // Generic fallback
            _ => (
                StatusCodes.Status500InternalServerError,
                CreateGenericErrorResponse(exception)
            )
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        
        await context.Response.WriteAsJsonAsync(response);
    }

    private ApiResponse<object> CreateDatabaseErrorResponse(DbUpdateException ex)
    {
        // Log completo do erro
        _logger.LogError(ex, "💥 Database error");
        
        // Não vaza detalhes em produção
        var message = _env.IsDevelopment() 
            ? $"Database error: {ex.InnerException?.Message ?? ex.Message}"
            : "A database error occurred";
            
        return ApiResponse<object>.Fail(message);
    }

    private ApiResponse<object> CreateGenericErrorResponse(Exception ex)
    {
        _logger.LogCritical(ex, "💥 Critical unhandled exception");
        
        var message = _env.IsDevelopment()
            ? $"Internal server error: {ex.Message}"
            : "An unexpected error occurred";
            
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
3. Command Handlers (Application Layer)
Use try-catch apenas quando necessário para transformar exceções:
csharp// Application/Commands/Products/CreateProduct/CreateProductCommandHandler.cs
public class CreateProductCommandHandler 
    : ICommandHandler<CreateProductCommand, ApiResponse<CreateProductResponse>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateProductCommandValidator _validator;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public async Task<ApiResponse<CreateProductResponse>> HandleAsync(
        CreateProductCommand request, 
        CancellationToken cancellationToken = default)
    {
        // 1. Validação com Validator
        var validationResult = _validator.Validate(request);
        if (validationResult.HasErrors)
        {
            _logger.LogWarning("⚠️ Validation failed for CreateProduct: {Errors}", 
                string.Join(", ", validationResult.Errors.Select(e => e.Message)));
            return ApiResponse<CreateProductResponse>.Fail(validationResult.Errors.ToList());
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // 2. Verificar duplicação (pode lançar DuplicateSlugException)
            var existing = await _productRepository.FindAsync(
                p => p.Slug == request.Slug, 
                cancellationToken);
                
            if (existing.Any())
            {
                // Opção 1: Retornar ApiResponse diretamente
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApiResponse<CreateProductResponse>.Fail("Product with this slug already exists");
                
                // Opção 2: Lançar exceção (será capturada pelo middleware)
                // throw new DuplicateSlugException(request.Slug);
            }

            // 3. Criar produto (validações de domínio podem lançar exceções)
            var price = Money.Create(request.Price, request.Currency);
            var product = Product.Create(
                request.Name,
                request.Slug,
                price,
                request.Stock,
                request.Description,
                request.ShortDescription,
                request.CategoryId,
                request.Sku,
                request.IsActive
            );

            // 4. Persistir
            await _productRepository.AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("✅ Product created: {ProductId} - {ProductName}", 
                product.Id, product.Name);

            // 5. Retornar sucesso
            var response = new CreateProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                // ... outros campos
            };

            return ApiResponse<CreateProductResponse>.Ok(
                response, 
                "Product created successfully");
        }
        catch (DomainException ex)
        {
            // Rollback e deixa o middleware tratar
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogWarning(ex, "⚠️ Domain validation failed");
            throw; // Middleware converte para ApiResponse
        }
        catch (Exception ex)
        {
            // Rollback em caso de erro inesperado
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "💥 Unexpected error creating product");
            throw; // Middleware converte para ApiResponse
        }
    }
}
4. Controllers (API Layer)
Controllers simples, deixam o middleware tratar exceções:
csharp// Api/Controllers/ProductController.cs
[HttpPost]
[ProducesResponseType(typeof(ApiResponse<CreateProductResponse>), 201)]
[ProducesResponseType(typeof(ApiResponse<CreateProductResponse>), 400)]
[ProducesResponseType(typeof(ApiResponse<CreateProductResponse>), 409)]
[ProducesResponseType(typeof(ApiResponse<CreateProductResponse>), 500)]
public async Task<IActionResult> CreateProduct(
    [FromBody] CreateProductCommand command, 
    CancellationToken cancellationToken = default)
{
    _logger.LogInformation("📝 Creating product: {ProductName}", command.Name);

    // Não precisa de try-catch aqui!
    // O middleware captura tudo
    var result = await _mediator.SendAsync<ApiResponse<CreateProductResponse>>(
        command, 
        cancellationToken);

    if (!result.Success)
    {
        _logger.LogWarning("❌ Failed to create product: {Errors}", 
            string.Join(", ", result.Errors?.Select(e => e.Message) ?? []));
        return BadRequest(result);
    }

    _logger.LogInformation("✅ Product created successfully: {ProductId}", result.Data.Id);
    
    return CreatedAtAction(
        nameof(GetProductById),
        new { id = result.Data.Id },
        result);
}
5. Configuração no Program.cs
csharp// Program.cs

// ORDEM IMPORTANTÍSSIMA!
var app = builder.Build();

// 1. Logging de requisições (primeiro)
app.UseSerilogRequestLogging();

// 2. Exception Handler (logo após logging)
app.UseGlobalExceptionHandler();

// 3. CORS
app.UseCors("AllowFrontendApps");

// 4. HTTPS Redirection
app.UseHttpsRedirection();

// 5. Swagger (dev only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 6. Authentication/Authorization
app.UseAuthentication();
app.UseAuthorization();

// 7. Controllers
app.MapControllers();
```

## 🎨 Estrutura de Exceções Recomendada
```
CatalogService.Domain/
├── Exceptions/
│   ├── CatalogDomainException.cs (base)
│   ├── Products/
│   │   ├── ProductNotFoundException.cs
│   │   ├── DuplicateSlugException.cs
│   │   ├── DuplicateSkuException.cs
│   │   ├── InsufficientStockException.cs
│   │   └── InvalidPriceException.cs
│   ├── Categories/
│   │   ├── CategoryNotFoundException.cs
│   │   ├── CircularCategoryReferenceException.cs
│   │   └── CategoryHasProductsException.cs
│   └── Reviews/
│       ├── DuplicateReviewException.cs
│       └── ReviewNotAllowedException.cs

CatalogService.Api/
├── Middlewares/
│   └── GlobalExceptionHandlerMiddleware.cs
✅ Benefícios desta Abordagem

Separação Clara: Domínio lança exceções, Application coordena, API padroniza
Código Limpo: Controllers e handlers sem try-catch desnecessários
Consistência: Todas as respostas seguem o padrão ApiResponse
Rastreabilidade: Logs estruturados em cada camada
Testabilidade: Fácil testar exceções de domínio isoladamente
Manutenibilidade: Um único ponto para mapear exceções → HTTP status codes

📋 Diretrizes de Uso
✅ LANCE EXCEÇÕES quando:

Violação de regra de domínio (ex: estoque insuficiente)
Entidade não encontrada
Operação inválida no estado atual
Dados inconsistentes que impedem a operação

❌ NÃO LANCE EXCEÇÕES para:

Validação de input (use ValidationHandler)
Fluxos de negócio esperados (ex: cupom expirado → retorne ApiResponse)
Erros de autorização (use AuthorizationFilter)

Essa abordagem está alinhada com seu projeto e aproveita o ApiResponse que você já implementou! 🚀Tentar novamenteBDanalise a estrutura e o fluxo dos meus commands desde o controller🔍 Análise Detalhada do Fluxo de Commands
Vou analisar o fluxo completo desde o Controller até o Domain, identificando pontos fortes e oportunidades de melhoria.
📊 Fluxo Atual Analisado
1️⃣ Controller Layer ✅ BOM
csharp// CategoryController.cs (linhas relevantes)
[HttpPost]
public async Task<IActionResult> CreateCategory(
    [FromBody] CreateCategoryCommand command, 
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("📝 Iniciando criação de categoria: {CategoryName}", command.Name);

        // ✅ BOM: Validação de ModelState
        if (!ModelState.IsValid)
        {
            var errorHandler = new ValidationHandler();
            errorHandler.Add("Dados inválidos.");
            return BadRequest(ApiResponse<CreateCategoryResponse>.Fail(errorHandler.Errors.ToList()));
        }

        // ✅ BOM: Usa Mediator
        var result = await _mediator.SendAsync<ApiResponse<CreateCategoryResponse>>(
            command, cancellationToken);

        if (result.Success && result.Data != null)
        {
            return CreatedAtAction(nameof(CreateCategory), new { id = result.Data.Id }, result);
        }

        return BadRequest(result);
    }
    catch (ArgumentException ex)
    {
        // ⚠️ PROBLEMA: Captura específica desnecessária
        return BadRequest(ApiResponse<CreateCategoryResponse>.Fail(...));
    }
    catch (Exception ex)
    {
        // ⚠️ PROBLEMA: Tratamento genérico no controller
        return StatusCode(500, ApiResponse<CreateCategoryResponse>.Fail(...));
    }
}
❌ Problemas Identificados:

Try-catch excessivo: Controller não deveria tratar exceções
ModelState validation: Redundante com validator
Exception handling duplicado: Entre controller e middleware
Logging verboso: Muitos emojis e detalhes


2️⃣ Command ✅ EXCELENTE
csharp// CreateCategoryCommand.cs
public class CreateCategoryCommand : ICommand<ApiResponse<CreateCategoryResponse>>
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public string Metadata { get; set; } = "{}";
}
✅ Pontos Fortes:

DTO simples e focado
Properties com valores default apropriados
Implementa ICommand<TResponse> corretamente

🔄 Sugestões:
csharp// Adicionar data annotations para validação básica
public class CreateCategoryCommand : ICommand<ApiResponse<CreateCategoryResponse>>
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug é obrigatório")]
    [MaxLength(200)]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", 
        ErrorMessage = "Slug deve conter apenas letras minúsculas, números e hífens")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public Guid? ParentId { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    [Required]
    public string Metadata { get; set; } = "{}";
}

3️⃣ Validator ✅ BOM (mas pode melhorar)
csharp// CreateCategoryCommandValidator.cs
public class CreateCategoryCommandValidator
{
    public ValidationHandler Validate(CreateCategoryCommand command)
    {
        var handler = new ValidationHandler();
        
        if (string.IsNullOrWhiteSpace(command.Name))
            handler.Add("Nome da categoria é obrigatório");
        else if (command.Name.Length > 200)
            handler.Add("Nome da categoria deve ter no máximo 200 caracteres");
        
        // ... mais validações
        
        return handler;
    }
}
⚠️ Problemas:

Não usa FluentValidation: Framework padrão da indústria
Lógica duplicada: Mesmas regras que Data Annotations
Validação de JSON manual: Poderia ser mais robusta

✨ Refatoração Recomendada:
csharp// Instalar: FluentValidation.AspNetCore
public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    private readonly ICategoryRepository _categoryRepository;

    public CreateCategoryCommandValidator(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome da categoria é obrigatório")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres")
            .MustAsync(BeUniqueNameAsync).WithMessage("Já existe uma categoria com este nome")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug é obrigatório")
            .MaximumLength(200).WithMessage("Slug deve ter no máximo 200 caracteres")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
                .WithMessage("Slug inválido")
            .MustAsync(BeUniqueSlugAsync).WithMessage("Slug já existe");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Metadata)
            .NotEmpty()
            .Must(BeValidJson).WithMessage("Metadata deve ser um JSON válido");

        RuleFor(x => x.ParentId)
            .MustAsync(ParentExistsAsync).WithMessage("Categoria pai não encontrada")
            .When(x => x.ParentId.HasValue);
    }

    private async Task<bool> BeUniqueNameAsync(string name, CancellationToken ct)
    {
        var existing = await _categoryRepository.FindAsync(c => c.Name == name, ct);
        return !existing.Any();
    }

    private async Task<bool> BeUniqueSlugAsync(CreateCategoryCommand command, string slug, CancellationToken ct)
    {
        var existing = await _categoryRepository.FindAsync(c => c.Slug == slug, ct);
        return !existing.Any();
    }

    private async Task<bool> ParentExistsAsync(Guid? parentId, CancellationToken ct)
    {
        if (!parentId.HasValue) return true;
        var parent = await _categoryRepository.GetByIdAsync(parentId.Value, ct);
        return parent != null;
    }

    private bool BeValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

4️⃣ Command Handler ⚠️ CRÍTICO - Precisa Melhorar
csharp// CreateCategoryCommandHandler.cs
public class CreateCategoryCommandHandler 
    : ICommandHandler<CreateCategoryCommand, ApiResponse<CreateCategoryResponse>>
{
    public async Task<ApiResponse<CreateCategoryResponse>> HandleAsync(
        CreateCategoryCommand request, 
        CancellationToken cancellationToken = default)
    {
        // ❌ PROBLEMA 1: Validação DENTRO do handler
        var validationResult = _validator.Validate(request);
        if (validationResult.HasErrors)
            return ApiResponse<CreateCategoryResponse>.Fail(validationResult.Errors.ToList());

        // ❌ PROBLEMA 2: Try-catch genérico capturando tudo
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // ❌ PROBLEMA 3: Validação de negócio no handler, não no domínio
            var existingCategories = await _categoryRepository
                .FindAsync(c => c.Slug == request.Slug, cancellationToken);
            
            if (existingCategories.Any())
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                validationResult.Add("Já existe uma categoria com este slug.");
                return ApiResponse<CreateCategoryResponse>.Fail(...);
            }

            // ✅ BOM: Usa factory method do domínio
            var category = Category.Create(
                request.Name,
                request.Slug,
                request.Description,
                request.ParentId,
                request.DisplayOrder,
                request.IsActive,
                request.Metadata
            );

            await _categoryRepository.AddAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // ✅ BOM: Mapeamento manual claro
            var response = new CreateCategoryResponse { ... };
            return ApiResponse<CreateCategoryResponse>.Ok(response, "...");
        }
        catch (ArgumentException ex)
        {
            // ❌ PROBLEMA 4: Tratamento duplicado de exceções
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ApiResponse<CreateCategoryResponse>.Fail(...);
        }
        catch (Exception ex)
        {
            // ❌ PROBLEMA 5: Engolindo exceções genéricas
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ApiResponse<CreateCategoryResponse>.Fail($"Erro interno: {ex.Message}");
        }
    }
}
❌ Problemas Críticos:

Validação no Handler: Deveria ser no pipeline (Behavior)
Lógica de Negócio Vazada: Verificação de slug duplicado deveria estar no validator ou domínio
Try-Catch Excessivo: Middleware deveria capturar
Transação Manual: Deveria ser automática via Behavior
Rollback Manual: Deveria ser automático em caso de exceção


5️⃣ Domain Entity ✅ EXCELENTE (mas com ressalvas)
csharp// Category.cs
public class Category : AggregateRoot
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    // ... outras propriedades

    private Category() { } // ✅ BOM: EF Constructor

    // ✅ EXCELENTE: Factory Method
    public static Category Create(
        string name,
        string slug,
        string? description = null,
        Guid? parentId = null,
        int displayOrder = 0,
        bool isActive = true,
        string metadata = "{}")
    {
        // ❌ PROBLEMA: Validações comentadas ou removidas
        // Deveria validar AQUI, não no validator
        return new Category
        {
            Name = name,
            Slug = slug,
            Description = description,
            ParentId = parentId,
            IsActive = isActive,
            DisplayOrder = displayOrder,
            Metadata = metadata,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // ✅ BOM: Método Validate separado
    public override ValidationHandler Validate(ValidationHandler handler)
    {
        if (string.IsNullOrWhiteSpace(Name))
            handler.Add("Nome da categoria é obrigatório");
        
        // ... mais validações
        
        return handler;
    }
}
⚠️ Problema: Validações estão no método Validate(), mas não são chamadas automaticamente no Create().
✨ Refatoração Recomendada:
csharppublic static Category Create(
    string name,
    string slug,
    string? description = null,
    Guid? parentId = null,
    int displayOrder = 0,
    bool isActive = true,
    string metadata = "{}")
{
    // ✅ Validar SEMPRE na criação
    var category = new Category
    {
        Name = name,
        Slug = slug,
        Description = description,
        ParentId = parentId,
        IsActive = isActive,
        DisplayOrder = displayOrder,
        Metadata = metadata,
        Version = 1,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    // ✅ Validação automática
    var validationHandler = new ValidationHandler();
    category.Validate(validationHandler);
    validationHandler.ThrowIfHasErrors(); // ❌ Lança DomainException

    return category;
}
```

---

## 🎯 Proposta de Fluxo Otimizado

### **Estrutura Ideal com Pipeline Behaviors**
```
┌──────────────┐
│  Controller  │  ← Remove try-catch, apenas chama Mediator
└──────┬───────┘
       │
       ▼
┌──────────────────────┐
│  MediatR Pipeline    │
├──────────────────────┤
│ 1. LoggingBehavior   │  ← Log entrada/saída
│ 2. ValidationBehavior│  ← FluentValidation automática
│ 3. TransactionBehavior│ ← BeginTransaction/Commit automático
│ 4. ExceptionBehavior │  ← Captura exceções e retorna ApiResponse
└──────┬───────────────┘
       │
       ▼
┌──────────────┐
│ Command      │  ← Handler limpo, só lógica de negócio
│ Handler      │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│   Domain     │  ← Validações de invariantes
│   Entity     │
└──────────────┘
1. Validation Behavior (Pipeline)
csharp// Application/Behaviors/ValidationBehavior.cs
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any()) 
            return await next();

        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            _logger.LogWarning("⚠️ Validation failed for {CommandType}: {Errors}",
                typeof(TRequest).Name,
                string.Join(", ", failures.Select(f => f.ErrorMessage)));

            var errors = failures
                .Select(f => new Error(f.ErrorMessage))
                .ToList();

            // ✅ Retorna ApiResponse tipado
            return CreateFailResponse<TResponse>(errors);
        }

        return await next();
    }

    private static TResponse CreateFailResponse<T>(List<Error> errors)
    {
        var responseType = typeof(T);
        
        if (responseType.IsGenericType && 
            responseType.GetGenericTypeDefinition() == typeof(ApiResponse<>))
        {
            var dataType = responseType.GetGenericArguments()[0];
            var failMethod = typeof(ApiResponse<>)
                .MakeGenericType(dataType)
                .GetMethod(nameof(ApiResponse<object>.Fail), new[] { typeof(List<Error>) });
            
            return (T)failMethod!.Invoke(null, new object[] { errors })!;
        }

        throw new InvalidOperationException($"Cannot create fail response for type {responseType}");
    }
}
2. Transaction Behavior (Pipeline)
csharp// Application/Behaviors/TransactionBehavior.cs
public class TransactionBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TRequest).Name;

        try
        {
            _logger.LogDebug("🔄 Beginning transaction for {CommandName}", commandName);
            
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            
            var response = await next();
            
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            
            _logger.LogDebug("✅ Transaction committed for {CommandName}", commandName);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Transaction failed for {CommandName}, rolling back", commandName);
            
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            
            throw; // Re-throw para ExceptionBehavior ou middleware tratar
        }
    }
}
3. Logging Behavior (Pipeline)
csharp// Application/Behaviors/LoggingBehavior.cs
public class LoggingBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("📥 Executing {CommandName}", commandName);

        try
        {
            var response = await next();
            
            stopwatch.Stop();
            
            _logger.LogInformation("✅ {CommandName} executed successfully in {ElapsedMs}ms",
                commandName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "❌ {CommandName} failed after {ElapsedMs}ms",
                commandName, stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
}
4. Command Handler Refatorado ✨
csharp// CreateCategoryCommandHandler.cs - VERSÃO LIMPA
public class CreateCategoryCommandHandler 
    : ICommandHandler<CreateCategoryCommand, ApiResponse<CreateCategoryResponse>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        ILogger<CreateCategoryCommandHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<CreateCategoryResponse>> HandleAsync(
        CreateCategoryCommand request, 
        CancellationToken cancellationToken = default)
    {
        // ✅ SEM validação - feita no Behavior
        // ✅ SEM transação - gerenciada no Behavior
        // ✅ SEM try-catch - tratado no Middleware
        // ✅ SEM logging excessivo - feito no Behavior

        // Apenas lógica de negócio pura
        var category = Category.Create(
            request.Name,
            request.Slug,
            request.Description,
            request.ParentId,
            request.DisplayOrder,
            request.IsActive,
            request.Metadata
        );

        await _categoryRepository.AddAsync(category, cancellationToken);
        
        // SaveChanges é chamado automaticamente pelo TransactionBehavior
        // antes do Commit

        var response = new CreateCategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ParentId = category.ParentId,
            IsActive = category.IsActive,
            DisplayOrder = category.DisplayOrder,
            CreatedAt = category.CreatedAt
        };

        return ApiResponse<CreateCategoryResponse>.Ok(
            response, 
            "Categoria criada com sucesso");
    }
}
5. Controller Refatorado ✨
csharp// CategoryController.cs - VERSÃO LIMPA
[HttpPost]
[ProducesResponseType(typeof(ApiResponse<CreateCategoryResponse>), 201)]
[ProducesResponseType(typeof(ApiResponse<CreateCategoryResponse>), 400)]
public async Task<IActionResult> CreateCategory(
    [FromBody] CreateCategoryCommand command, 
    CancellationToken cancellationToken = default)
{
    // ✅ SEM try-catch
    // ✅ SEM validação manual
    // ✅ SEM logging excessivo
    
    var result = await _mediator.SendAsync<ApiResponse<CreateCategoryResponse>>(
        command, cancellationToken);

    return result.Success 
        ? CreatedAtAction(nameof(GetCategoryById), new { id = result.Data.Id }, result)
        : BadRequest(result);
}
6. Configuração no Program.cs
csharp// Program.cs - Configuração de Behaviors

builder.Services.AddMediator(typeof(CreateCategoryCommandHandler).Assembly)
    .AddBehaviors(); // Extension method

// Extension Method
public static class MediatorExtensions
{
    public static IServiceCollection AddBehaviors(this IServiceCollection services)
    {
        // Ordem importa! Executam na ordem registrada
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        
        return services;
    }
}

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(CreateCategoryCommandValidator).Assembly);
```

---

## 📋 Checklist de Refatoração

### ✅ Prioridade Alta

- [ ] Implementar `ValidationBehavior` com FluentValidation
- [ ] Implementar `TransactionBehavior` automático
- [ ] Remover try-catch dos Controllers
- [ ] Remover validação manual dos Handlers
- [ ] Adicionar `GlobalExceptionHandlerMiddleware`

### ✅ Prioridade Média

- [ ] Implementar `LoggingBehavior`
- [ ] Refatorar validações do domínio para serem chamadas no `Create()`
- [ ] Criar exceções de domínio específicas
- [ ] Adicionar Data Annotations nos Commands

### ✅ Prioridade Baixa

- [ ] Implementar `PerformanceBehavior` (métricas)
- [ ] Adicionar `CachingBehavior` (para queries)
- [ ] Implementar `RetryBehavior` (resilience)

---

## 🎯 Resultado Final

**ANTES:**
```
Controller (90 linhas) 
  → Handler (120 linhas) 
    → Domain (30 linhas)
    
Total: 240 linhas
Responsabilidades: Duplicadas em cada camada
```

**DEPOIS:**
```
Controller (10 linhas)
  → Pipeline Behaviors (3 x 40 linhas = 120 linhas, reutilizáveis)
    → Handler (25 linhas)
      → Domain (35 linhas)

Total: 190 linhas
Responsabilidades: Separadas e reutilizáveis
Benefícios:

✅ 20% menos código
✅ 0 duplicação
✅ Behaviors reutilizáveis em TODOS os commands
✅ Handlers focados apenas em lógica de negócio
✅ Controllers limpos e simples
✅ Domínio auto-validável

Quer que eu implemente algum desses behaviors especificamente? 🚀