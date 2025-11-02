using System.Linq;
using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;

namespace CatalogService.Application.Commands.Categories.CreateCategory;

public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, ApiResponse<CreateCategoryResponse>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepository, 
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<CreateCategoryResponse>> HandleAsync(CreateCategoryCommand request, CancellationToken cancellationToken = default)
    {
        // Validação será feita automaticamente pelo ValidationBehavior
        
        // 1. Validar se já existe categoria com o mesmo slug
        var existingCategories = await _categoryRepository.FindAsync(c => c.Slug == request.Slug, cancellationToken);
        if (existingCategories.Any())
        {
            throw new DomainException("Já existe uma categoria com este slug.");
        }

        // 2. Criar a categoria usando o método factory
        var category = Category.Create(
            request.Name,
            request.Slug,
            request.Description,
            request.ParentId,
            request.DisplayOrder,
            request.IsActive,
            request.Metadata
        );

        // 3. Salvar no repositório
        await _categoryRepository.AddAsync(category, cancellationToken);

        // 4. Persistir mudanças no banco (TransactionBehavior gerencia a transação automaticamente)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Criar resposta de sucesso
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
}