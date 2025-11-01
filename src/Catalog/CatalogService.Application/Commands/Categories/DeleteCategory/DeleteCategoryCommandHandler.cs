using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
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
        try
        {
            _logger.LogInformation("Iniciando exclusão da categoria {CategoryId}", request.Id);

            // 1. Validar o comando
            var validationResult = _validator.Validate(request);
            if (validationResult.HasErrors)
            {
                _logger.LogWarning("Comando de exclusão de categoria inválido: {Errors}", 
                    string.Join(", ", validationResult.Errors));
                return ApiResponse<DeleteCategoryResponse>.Fail(validationResult.Errors.ToList());
            }

            // 2. Iniciar transação
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // 3. Buscar a categoria existente
                var existingCategory = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
                if (existingCategory == null)
                {
                    _logger.LogWarning("Categoria não encontrada: {CategoryId}", request.Id);
                    validationResult.Add("A categoria especificada não existe");
                    return ApiResponse<DeleteCategoryResponse>.Fail(validationResult.Errors.ToList());
                }

                // 4. Verificar se a categoria já está inativa (já foi deletada)
                if (!existingCategory.IsActive)
                {
                    _logger.LogWarning("Categoria já está inativa: {CategoryId}", request.Id);
                    validationResult.Add("A categoria já foi deletada anteriormente");
                    return ApiResponse<DeleteCategoryResponse>.Fail(validationResult.Errors.ToList());
                }

                // 5. Verificar se existem subcategorias ativas
                var activeSubcategories = await _categoryRepository.FindAsync(
                    c => c.ParentId == request.Id && c.IsActive, 
                    cancellationToken);

                if (activeSubcategories.Any())
                {
                    _logger.LogWarning("Categoria possui subcategorias ativas: {CategoryId}", request.Id);
                    validationResult.Add("Não é possível deletar uma categoria que possui subcategorias ativas");
                    return ApiResponse<DeleteCategoryResponse>.Fail(validationResult.Errors.ToList());
                }

                // 6. TODO: Verificar se existem produtos associados (quando implementado)
                // Esta validação será implementada quando o módulo de produtos estiver disponível
                // var productsInCategory = await _productRepository.FindAsync(p => p.CategoryId == request.Id, cancellationToken);
                // if (productsInCategory.Any())
                // {
                //     _logger.LogWarning("Categoria possui produtos associados: {CategoryId}", request.Id);
                //     validationResult.Add("Não é possível deletar uma categoria que possui produtos associados");
                //     return ApiResponse<DeleteCategoryResponse>.Fail(validationResult.Errors.ToList());
                // }

                // 7. Realizar o soft delete
                existingCategory.SoftDelete();

                // 8. Atualizar no repositório
                _categoryRepository.Update(existingCategory);

                // 9. Salvar as mudanças
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 10. Confirmar a transação
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Categoria deletada com sucesso: {CategoryId}", existingCategory.Id);

                // 11. Retornar a resposta
                var response = new DeleteCategoryResponse(
                    success: true,
                    categoryId: existingCategory.Id,
                    message: "Categoria deletada com sucesso");

                return ApiResponse<DeleteCategoryResponse>.Ok(response, "Categoria deletada com sucesso");
            }
            catch (Exception)
            {
                // Rollback em caso de erro
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (DomainException ex)
        {
            _logger.LogError(ex, "Erro de domínio ao deletar categoria {CategoryId}", request.Id);
            var errorHandler = new BuildingBlocks.Core.Validations.ValidationHandler();
            errorHandler.Add(ex.Message);
            return ApiResponse<DeleteCategoryResponse>.Fail(errorHandler.Errors.ToList());
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Argumento inválido ao deletar categoria {CategoryId}", request.Id);
            var errorHandler = new BuildingBlocks.Core.Validations.ValidationHandler();
            errorHandler.Add(ex.Message);
            return ApiResponse<DeleteCategoryResponse>.Fail(errorHandler.Errors.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao deletar categoria {CategoryId}", request.Id);
            var errorHandler = new BuildingBlocks.Core.Validations.ValidationHandler();
            errorHandler.Add("Ocorreu um erro inesperado. Tente novamente.");
            return ApiResponse<DeleteCategoryResponse>.Fail(errorHandler.Errors.ToList());
        }
    }
}