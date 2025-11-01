using System.Linq;
using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;

namespace CatalogService.Application.Commands.Categories.CreateCategory;

public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, ApiResponse<CreateCategoryResponse>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateCategoryCommandValidator _validator;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepository, 
        IUnitOfWork unitOfWork,
        CreateCategoryCommandValidator validator)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<ApiResponse<CreateCategoryResponse>> HandleAsync(CreateCategoryCommand request, CancellationToken cancellationToken = default)
    {
        // 1. Validar o comando ANTES de qualquer operação
        var validationResult = _validator.Validate(request);
        if (validationResult.HasErrors)
        {
            return ApiResponse<CreateCategoryResponse>.Fail(validationResult.Errors.ToList());
        }

        // 2. Iniciar transação explícita
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Validar se já existe categoria com o mesmo slug
            var existingCategories = await _categoryRepository.FindAsync(c => c.Slug == request.Slug, cancellationToken);
            if (existingCategories.Any())
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                validationResult.Add("Já existe uma categoria com este slug.");
                return ApiResponse<CreateCategoryResponse>.Fail(validationResult.Errors.ToList());
            }

            // Criar a categoria usando o método factory
            var category = Category.Create(
                request.Name,
                request.Slug,
                request.Description,
                request.ParentId,
                request.DisplayOrder,
                request.IsActive,
                request.Metadata
            );

            // Salvar no repositório
            await _categoryRepository.AddAsync(category, cancellationToken);

            // Persistir mudanças no banco
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Confirmar transação
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Criar resposta de sucesso
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

            return ApiResponse<CreateCategoryResponse>.Ok(response, "Categoria criada com sucesso.");
        }
        catch (ArgumentException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            validationResult.Add(ex.Message);
            return ApiResponse<CreateCategoryResponse>.Fail(validationResult.Errors.ToList());
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            validationResult.Add($"Erro interno do servidor: {ex.Message}");
            return ApiResponse<CreateCategoryResponse>.Fail(validationResult.Errors.ToList());
        }
    }
}