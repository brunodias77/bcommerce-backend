using BuildingBlocks.Core.Data;
using BuildingBlocks.Core.Exceptions;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Repository;
using Microsoft.Extensions.Logging;

namespace CatalogService.Application.Commands.Products.DeleteProduct;

/// <summary>
/// Handler para o comando de deletar produto
/// </summary>
public class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, ApiResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteProductCommandHandler> logger)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executa o comando de deletar produto
    /// </summary>
    /// <param name="request">Comando de deletar</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da operação</returns>
    public async Task<ApiResponse<bool>> HandleAsync(DeleteProductCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [DeleteProductCommandHandler] Iniciando exclusão do produto {ProductId}", request.Id);

        try
        {
            // Buscar produto
            var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
            
            if (product == null)
            {
                _logger.LogWarning("❌ [DeleteProductCommandHandler] Produto {ProductId} não encontrado", request.Id);
                throw new KeyNotFoundException($"Produto com ID {request.Id} não foi encontrado");
            }

            // Verificar se já está deletado
            if (product.DeletedAt.HasValue)
            {
                _logger.LogWarning("❌ [DeleteProductCommandHandler] Produto {ProductId} já foi deletado em {DeletedAt}", 
                    request.Id, product.DeletedAt);
                throw new DomainException("Produto já foi removido anteriormente");
            }

            // TODO: Validar se não há pedidos pendentes
            // Por enquanto, assumir que não há integração com Order Service

            // Realizar soft delete
            product.SoftDelete();

            // Atualizar no repositório
            _productRepository.Update(product);

            // Persistir alterações
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ [DeleteProductCommandHandler] Produto {ProductId} excluído com sucesso", request.Id);

            return ApiResponse<bool>.Ok(true, "Produto removido com sucesso");
        }
        catch (Exception ex) when (!(ex is KeyNotFoundException || ex is DomainException))
        {
            _logger.LogError(ex, "❌ [DeleteProductCommandHandler] Erro ao excluir produto {ProductId}", request.Id);
            throw;
        }
    }
}