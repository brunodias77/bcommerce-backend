using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.Categories.ActivateCategory;

public class ActivateCategoryCommandHandler : ICommandHandler<ActivateCategoryCommand, ApiResponse<ActivateCategoryResponse>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActivateCategoryCommandHandler> _logger;

    public ActivateCategoryCommandHandler(
        ICategoryRepository categoryRepository, 
        IUnitOfWork unitOfWork,
        ILogger<ActivateCategoryCommandHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<ActivateCategoryResponse>> HandleAsync(ActivateCategoryCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [ActivateCategoryCommandHandler] Iniciando processamento para ActivateCategoryCommand");
        
        // 1. Buscar categoria por ID
        var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
        // 2. Verificar se a categoria existe
        if (category == null)
        {
            throw new KeyNotFoundException("A categoria especificada não existe");
        }

        // 2. Verificar se a categoria já está ativa
        if (category.IsActive)
        {
            throw new DomainException("A categoria já está ativa");
        }

        // 3. Ativar a categoria
        category.Activate();

        // 4. Atualizar no repositório
        _categoryRepository.Update(category);

        // 5. Persistir mudanças no banco (TransactionBehavior gerencia a transação automaticamente)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Criar resposta de sucesso
        var response = new ActivateCategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            IsActive = category.IsActive,
            UpdatedAt = category.UpdatedAt
        };

        _logger.LogInformation("✅ [ActivateCategoryCommandHandler] Processamento concluído com sucesso para ActivateCategoryCommand");
        
        return ApiResponse<ActivateCategoryResponse>.Ok(response, "Categoria ativada com sucesso.");
    }
}