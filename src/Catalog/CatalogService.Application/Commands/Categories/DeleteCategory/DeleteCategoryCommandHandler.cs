using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.Categories.DeleteCategory;

/// <summary>
/// Handler para o comando de deletar categoria
/// </summary>
public class DeleteCategoryCommandHandler : ICommandHandler<DeleteCategoryCommand, ApiResponse<DeleteCategoryResponse>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCategoryCommandHandler> _logger;
    private readonly DeleteCategoryCommandValidator _validator;

    public DeleteCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCategoryCommandHandler> logger)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validator = new DeleteCategoryCommandValidator();
    }

    /// <summary>
    /// Executa o comando de deletar categoria
    /// </summary>
    /// <param name="request">Comando de deletar</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da operação</returns>
    public async Task<ApiResponse<DeleteCategoryResponse>> HandleAsync(
        DeleteCategoryCommand request, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando exclusão da categoria {CategoryId}", request.Id);

        // 1. Validar o comando
        var validationResult = _validator.Validate(request);
        if (validationResult.HasErrors)
        {
            throw new ValidationException(validationResult.Errors.ToList());
        }

        // 2. Buscar a categoria existente
        var existingCategory = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
        if (existingCategory == null)
        {
            throw new KeyNotFoundException("A categoria especificada não existe");
        }

        // 3. Verificar se a categoria já está inativa (já foi deletada)
        if (!existingCategory.IsActive)
        {
            throw new InvalidOperationException("A categoria já foi deletada anteriormente");
        }

        // 4. Verificar se existem subcategorias ativas
        var activeSubcategories = await _categoryRepository.FindAsync(
            c => c.ParentId == request.Id && c.IsActive, 
            cancellationToken);

        if (activeSubcategories.Any())
        {
            throw new DomainException("Não é possível deletar uma categoria que possui subcategorias ativas");
        }

        // 5. TODO: Verificar se existem produtos associados (quando implementado)
        // Esta validação será implementada quando o módulo de produtos estiver disponível
        // var productsInCategory = await _productRepository.FindAsync(p => p.CategoryId == request.Id, cancellationToken);
        // if (productsInCategory.Any())
        // {
        //     throw new DomainException("Não é possível deletar uma categoria que possui produtos associados");
        // }

        // 6. Realizar o soft delete
        existingCategory.SoftDelete();

        // 7. Atualizar no repositório
        _categoryRepository.Update(existingCategory);

        // 8. Salvar as mudanças (TransactionBehavior gerencia a transação automaticamente)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Categoria deletada com sucesso: {CategoryId}", existingCategory.Id);

        // 9. Retornar a resposta
        var response = new DeleteCategoryResponse(
            success: true,
            categoryId: existingCategory.Id,
            message: "Categoria deletada com sucesso");

        return ApiResponse<DeleteCategoryResponse>.Ok(response, "Categoria deletada com sucesso");
    }
}