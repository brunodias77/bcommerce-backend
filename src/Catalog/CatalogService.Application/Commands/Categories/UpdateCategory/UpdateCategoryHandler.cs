using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.Categories.UpdateCategory;

/// <summary>
/// Handler para o comando de atualização de categoria
/// </summary>
public class UpdateCategoryHandler : ICommandHandler<UpdateCategoryCommand, ApiResponse<UpdateCategoryResponse>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateCategoryHandler> _logger;
    private readonly UpdateCategoryCommandValidator _validator;

    public UpdateCategoryHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateCategoryHandler> logger)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validator = new UpdateCategoryCommandValidator();
    }

    /// <summary>
    /// Executa o comando de atualização de categoria
    /// </summary>
    /// <param name="request">Comando de atualização</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da operação</returns>
    public async Task<ApiResponse<UpdateCategoryResponse>> HandleAsync(
        UpdateCategoryCommand request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando atualização da categoria {CategoryId}", request.Id);

            // 1. Validar o comando
            var validationResult = _validator.Validate(request);
            if (validationResult.HasErrors)
            {
                _logger.LogWarning("Comando de atualização de categoria inválido: {Errors}", 
                    string.Join(", ", validationResult.Errors));
                return ApiResponse<UpdateCategoryResponse>.Fail(validationResult.Errors.ToList());
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
                    return ApiResponse<UpdateCategoryResponse>.Fail(validationResult.Errors.ToList());
                }

                // 4. Verificar se o slug já existe em outra categoria (se foi alterado)
                if (existingCategory.Slug != request.Slug)
                {
                    var categoriesWithSlug = await _categoryRepository.FindAsync(
                        c => c.Slug == request.Slug && c.Id != request.Id, 
                        cancellationToken);

                    if (categoriesWithSlug.Any())
                    {
                        _logger.LogWarning("Slug já existe em outra categoria: {Slug}", request.Slug);
                        validationResult.Add("Já existe uma categoria com este slug");
                        return ApiResponse<UpdateCategoryResponse>.Fail(validationResult.Errors.ToList());
                    }
                }

                // 5. Atualizar a categoria
                existingCategory.Update(
                    name: request.Name,
                    slug: request.Slug,
                    description: request.Description,
                    parentId: request.ParentId,
                    displayOrder: request.DisplayOrder,
                    isActive: request.IsActive,
                    metadata: request.Metadata);

                // 6. Validar a entidade atualizada
                var entityValidation = existingCategory.Validate(new BuildingBlocks.Core.Validations.ValidationHandler());
                if (entityValidation.HasErrors)
                {
                    _logger.LogWarning("Categoria atualizada é inválida: {Errors}", 
                        string.Join(", ", entityValidation.Errors));
                    return ApiResponse<UpdateCategoryResponse>.Fail(entityValidation.Errors.ToList());
                }

                // 7. Atualizar no repositório
                _categoryRepository.Update(existingCategory);

                // 8. Salvar as mudanças
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 9. Confirmar a transação
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Categoria atualizada com sucesso: {CategoryId}", existingCategory.Id);

                // 10. Retornar a resposta
                var response = new UpdateCategoryResponse
                {
                    Id = existingCategory.Id,
                    Name = existingCategory.Name,
                    Slug = existingCategory.Slug,
                    Description = existingCategory.Description,
                    ParentId = existingCategory.ParentId,
                    IsActive = existingCategory.IsActive,
                    DisplayOrder = existingCategory.DisplayOrder,
                    Version = existingCategory.Version,
                    CreatedAt = existingCategory.CreatedAt,
                    UpdatedAt = existingCategory.UpdatedAt
                };

                return ApiResponse<UpdateCategoryResponse>.Ok(response, "Categoria atualizada com sucesso");
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
            _logger.LogError(ex, "Erro de domínio ao atualizar categoria {CategoryId}", request.Id);
            var errorHandler = new BuildingBlocks.Core.Validations.ValidationHandler();
            errorHandler.Add(ex.Message);
            return ApiResponse<UpdateCategoryResponse>.Fail(errorHandler.Errors.ToList());
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Argumento inválido ao atualizar categoria {CategoryId}", request.Id);
            var errorHandler = new BuildingBlocks.Core.Validations.ValidationHandler();
            errorHandler.Add(ex.Message);
            return ApiResponse<UpdateCategoryResponse>.Fail(errorHandler.Errors.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao atualizar categoria {CategoryId}", request.Id);
            var errorHandler = new BuildingBlocks.Core.Validations.ValidationHandler();
            errorHandler.Add("Ocorreu um erro inesperado. Tente novamente.");
            return ApiResponse<UpdateCategoryResponse>.Fail(errorHandler.Errors.ToList());
        }
    }
}