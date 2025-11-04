using BuildingBlocks.CQRS.Mediator;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Validations;
using BuildingBlocks.Core.Exceptions;
using CatalogService.Application.Commands.ProductImages.AddProductImage;
using CatalogService.Application.Commands.ProductImages.SetPrimaryProductImage;
using CatalogService.Application.Commands.ProductImages.UpdateProductImage;
using CatalogService.Application.Commands.ProductImages.ReorderProductImages;
using CatalogService.Application.Commands.Products.CreateProduct;
using CatalogService.Application.Commands.Products.UpdateProduct;
using CatalogService.Application.Commands.Products.DeleteProduct;
using CatalogService.Application.Commands.Products.ActivateProduct;
using CatalogService.Application.Commands.Products.DeactivateProduct;
using CatalogService.Application.Commands.Products.UpdateProductStock;
using CatalogService.Application.Commands.Products.UpdateProductPrice;
using CatalogService.Application.Commands.Products.FeatureProduct;
using CatalogService.Application.Commands.Products.UnfeatureProduct;
using CatalogService.Application.Commands.Products.DeleteProductImage;
using CatalogService.Application.Commands.ProductReviews.CreateProductReview;
using CatalogService.Application.Commands.ProductReviews.DeleteProductReview;
using CatalogService.Application.Commands.ProductReviews.UpdateProductReview;
using CatalogService.Application.Commands.ProductReviews;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Api.Controllers;

/// <summary>
/// Controller responsável pelos endpoints de gerenciamento de produtos
/// </summary>
[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductController> _logger;

    public ProductController(IMediator mediator, ILogger<ProductController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Cria um novo produto
    /// </summary>
    /// <param name="command">Dados do produto a ser criado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados do produto criado</returns>
    /// <response code="201">Produto criado com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="409">Produto com slug já existe</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateProductResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CreateProductResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CreateProductResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<CreateProductResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [ProductController] Iniciando criação para CreateProductCommand");

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<CreateProductResponse>>(command, cancellationToken);

        _logger.LogInformation("✅ [ProductController] Operação concluída com sucesso para CreateProductCommand");

        return CreatedAtAction(
            nameof(CreateProduct),
            new { id = result.Data.Id },
            result);
    }

    /// <summary>
    /// Atualiza um produto existente
    /// </summary>
    /// <param name="id">ID do produto a ser atualizado</param>
    /// <param name="command">Dados do produto a ser atualizado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados do produto atualizado</returns>
    /// <response code="200">Produto atualizado com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="409">Produto com slug já existe</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UpdateProductResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UpdateProductResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UpdateProductResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<UpdateProductResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid id, [FromBody] UpdateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "➡️ [ProductController] Iniciando atualização para UpdateProductCommand com ID {ProductId}", id);

        // Definir o ID do comando a partir da rota
        command.Id = id;

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<UpdateProductResponse>>(command, cancellationToken);

        _logger.LogInformation(
            "✅ [ProductController] Operação concluída com sucesso para UpdateProductCommand com ID {ProductId}", id);

        return Ok(result);
    }

    /// <summary>
    /// Remove um produto (soft delete)
    /// </summary>
    /// <param name="id">ID do produto a ser removido</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Confirmação da remoção</returns>
    /// <response code="200">Produto removido com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="409">Produto já foi removido anteriormente</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteProduct([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [ProductController] Iniciando remoção para DeleteProductCommand com ID {ProductId}",
            id);

        // Criar command com o ID da rota
        var command = new DeleteProductCommand { Id = id };

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<bool>>(command, cancellationToken);

        _logger.LogInformation(
            "✅ [ProductController] Operação concluída com sucesso para DeleteProductCommand com ID {ProductId}", id);

        return Ok(result);
    }

    /// <summary>
    /// Ativa um produto
    /// </summary>
    /// <param name="id">ID do produto a ser ativado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados do produto ativado</returns>
    /// <response code="200">Produto ativado com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="409">Produto já está ativo</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse<ActivateProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ActivateProductResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ActivateProductResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ActivateProductResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<ActivateProductResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ActivateProduct([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "➡️ [ProductController] Iniciando ativação para ActivateProductCommand com ID {ProductId}", id);

        // Criar command com o ID da rota
        var command = new ActivateProductCommand { ProductId = id };

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<ActivateProductResponse>>(command, cancellationToken);

        _logger.LogInformation(
            "✅ [ProductController] Operação concluída com sucesso para ActivateProductCommand com ID {ProductId}", id);

        return Ok(result);
    }

    /// <summary>
    /// Desativa um produto
    /// </summary>
    /// <param name="id">ID do produto a ser desativado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da operação de desativação do produto</returns>
    /// <response code="200">Produto desativado com sucesso</response>
    /// <response code="400">Dados inválidos fornecidos</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="409">Conflito - produto já está desativado ou foi deletado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<DeactivateProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeactivateProduct([FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔄 [ProductController] Iniciando DeactivateProductCommand para ID {ProductId}", id);

        // Criar command com o ID da rota
        var command = new DeactivateProductCommand { ProductId = id };

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<DeactivateProductResponse>>(command, cancellationToken);

        _logger.LogInformation(
            "✅ [ProductController] Operação concluída com sucesso para DeactivateProductCommand com ID {ProductId}",
            id);

        return Ok(result);
    }

    /// <summary>
    /// Atualiza o estoque de um produto
    /// </summary>
    /// <param name="id">ID do produto a ter o estoque atualizado</param>
    /// <param name="command">Dados da atualização de estoque</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados do produto com estoque atualizado</returns>
    /// <response code="200">Estoque do produto atualizado com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="409">Conflito - produto foi deletado ou operação inválida</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPatch("{id:guid}/stock")]
    [ProducesResponseType(typeof(ApiResponse<UpdateProductStockResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProductStock([FromRoute] Guid id,
        [FromBody] UpdateProductStockCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [ProductController] Iniciando UpdateProductStockCommand para ID {ProductId}", id);

        // Definir o ID do comando a partir da rota
        command.Id = id;

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<UpdateProductStockResponse>>(command, cancellationToken);

        _logger.LogInformation(
            "✅ [ProductController] Operação concluída com sucesso para UpdateProductStockCommand com ID {ProductId}",
            id);

        return Ok(result);
    }

    /// <summary>
    /// Atualiza o preço de um produto
    /// </summary>
    /// <param name="id">ID do produto a ter o preço atualizado</param>
    /// <param name="command">Dados da atualização de preço</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados do produto com preço atualizado</returns>
    /// <response code="200">Preço do produto atualizado com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="409">Conflito - produto foi deletado ou operação inválida</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPatch("{id:guid}/price")]
    [ProducesResponseType(typeof(ApiResponse<UpdateProductPriceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProductPrice([FromRoute] Guid id,
        [FromBody] UpdateProductPriceCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [ProductController] Iniciando UpdateProductPriceCommand para ID {ProductId}", id);

        // Definir o ID do comando a partir da rota
        command.Id = id;

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<UpdateProductPriceResponse>>(command, cancellationToken);

        _logger.LogInformation(
            "✅ [ProductController] Operação concluída com sucesso para UpdateProductPriceCommand com ID {ProductId}",
            id);

        return Ok(result);
    }

    /// <summary>
    /// Destaca um produto como em destaque
    /// </summary>
    /// <param name="id">ID do produto a ser destacado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados do produto destacado</returns>
    /// <response code="200">Produto destacado com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="409">Produto já está em destaque ou foi deletado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPatch("{id:guid}/feature")]
    [ProducesResponseType(typeof(ApiResponse<FeatureProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> FeatureProduct([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [ProductController] Iniciando FeatureProductCommand para ID {ProductId}", id);

        // Criar command com o ID da rota
        var command = new FeatureProductCommand { Id = id };

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<FeatureProductResponse>>(command, cancellationToken);

        _logger.LogInformation(
            "✅ [ProductController] Operação concluída com sucesso para FeatureProductCommand com ID {ProductId}", id);

        return Ok(result);
    }

    /// <summary>
    /// Remove um produto do destaque
    /// </summary>
    /// <param name="id">ID do produto a ser removido do destaque</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados do produto removido do destaque</returns>
    /// <response code="200">Produto removido do destaque com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="409">Produto não está em destaque ou foi deletado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPatch("{id:guid}/unfeature")]
    [ProducesResponseType(typeof(ApiResponse<UnfeatureProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UnfeatureProduct([FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [ProductController] Iniciando UnfeatureProductCommand para ID {ProductId}", id);

        // Criar command com o ID da rota
        var command = new UnfeatureProductCommand { Id = id };

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<UnfeatureProductResponse>>(command, cancellationToken);

        _logger.LogInformation(
            "✅ [ProductController] Operação concluída com sucesso para UnfeatureProductCommand com ID {ProductId}", id);

        return Ok(result);
    }

    /// <summary>
    /// Adiciona uma imagem a um produto
    /// </summary>
    /// <param name="id">ID do produto</param>
    /// <param name="command">Dados da imagem a ser adicionada</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados da imagem adicionada</returns>
    /// <response code="201">Imagem adicionada com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="409">Conflito - produto foi deletado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost("{id:guid}/images")]
    [ProducesResponseType(typeof(ApiResponse<AddProductImageResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddProductImage([FromRoute] Guid id, [FromBody] AddProductImageCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [ProductController] Iniciando AddProductImageCommand para ID {ProductId}", id);

        // Definir o ProductId do comando a partir da rota
        command.ProductId = id;

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<AddProductImageResponse>>(command, cancellationToken);

        _logger.LogInformation(
            "✅ [ProductController] Operação concluída com sucesso para AddProductImageCommand com ID {ProductId}", id);

        return CreatedAtAction(
            nameof(AddProductImage),
            new { id = result.Data.Id },
            result);
    }

    /// <summary>
    /// Atualiza uma imagem do produto
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="imageId">ID da imagem</param>
    /// <param name="command">Dados da imagem a ser atualizada</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados da imagem atualizada</returns>
    /// <response code="200">Imagem atualizada com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Produto ou imagem não encontrada</response>
    /// <response code="409">Conflito - produto foi deletado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPut("{productId:guid}/images/{imageId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateProductImageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProductImage([FromRoute] Guid productId, [FromRoute] Guid imageId,
        [FromBody] UpdateProductImageCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "➡️ [ProductController] Iniciando UpdateProductImageCommand para ProductId {ProductId} e ImageId {ImageId}",
            productId, imageId);

        // Definir o ProductId e Id do comando a partir da rota
        command.ProductId = productId;
        command.Id = imageId;

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<UpdateProductImageResponse>>(command, cancellationToken);

        _logger.LogInformation(
            "✅ [ProductController] Operação concluída com sucesso para UpdateProductImageCommand com ProductId {ProductId} e ImageId {ImageId}",
            productId, imageId);

        return Ok(result);
    }

    /// <summary>
    /// Remove uma imagem do produto
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="imageId">ID da imagem a ser removida</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Confirmação da remoção da imagem</returns>
    /// <response code="200">Imagem removida com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Produto ou imagem não encontrada</response>
    /// <response code="409">Conflito - produto foi deletado ou imagem não pode ser removida</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpDelete("{productId:guid}/images/{imageId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteProductImage([FromRoute] Guid productId, [FromRoute] Guid imageId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "🗑️ [ProductController] Iniciando DeleteProductImageCommand para ProductId {ProductId} e ImageId {ImageId}",
            productId, imageId);

        // Criar command com o ID da imagem da rota
        var command = new DeleteProductImageCommand { Id = imageId };

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<bool>>(command, cancellationToken);

        _logger.LogInformation(
            "✅ [ProductController] Operação concluída com sucesso para DeleteProductImageCommand com ProductId {ProductId} e ImageId {ImageId}",
            productId, imageId);

        return Ok(result);
    }

    /// <summary>
    /// Define uma imagem como principal de um produto
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="imageId">ID da imagem a ser definida como principal</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Confirmação da operação</returns>
    /// <response code="200">Imagem definida como principal com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Produto ou imagem não encontrados</response>
    /// <response code="409">Conflito - produto foi deletado ou imagem não pertence ao produto</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPatch("{productId:guid}/images/{imageId:guid}/set-primary")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetPrimaryProductImage([FromRoute] Guid productId, [FromRoute] Guid imageId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "🎯 [ProductController] Iniciando SetPrimaryProductImageCommand para ProductId {ProductId} e ImageId {ImageId}",
            productId, imageId);

        // Criar command com os IDs da rota
        var command = new SetPrimaryProductImageCommand
        {
            ProductId = productId,
            ImageId = imageId
        };

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<bool>>(command, cancellationToken);

        _logger.LogInformation(
            "✅ [ProductController] Operação concluída com sucesso para SetPrimaryProductImageCommand com ProductId {ProductId} e ImageId {ImageId}",
            productId, imageId);

        return Ok(result);
    }

    /// <summary>
    /// Reordena as imagens de um produto
    /// </summary>
    /// <param name="productId">ID do produto cujas imagens serão reordenadas</param>
    /// <param name="command">Lista com a nova ordem das imagens</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Confirmação da operação</returns>
    /// <response code="200">Imagens reordenadas com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="409">Conflito - produto foi deletado ou imagens não pertencem ao produto</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPatch("{productId:guid}/images/reorder")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReorderProductImages([FromRoute] Guid productId,
        [FromBody] ReorderProductImagesCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "🔄 [ProductController] Iniciando ReorderProductImagesCommand para ProductId {ProductId}", productId);

        // Definir o ID do produto a partir da rota
        command.ProductId = productId;

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<bool>>(command, cancellationToken);

        _logger.LogInformation(
            "✅ [ProductController] Operação concluída com sucesso para ReorderProductImagesCommand com ProductId {ProductId}",
            productId);

        return Ok(result);
    }

    /// <summary>
    /// Cria uma nova avaliação para um produto
    /// </summary>
    /// <param name="id">ID do produto a ser avaliado</param>
    /// <param name="command">Dados da avaliação do produto</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados da avaliação criada</returns>
    /// <response code="201">Avaliação criada com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="409">Usuário já avaliou este produto</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost("{id:guid}/reviews")]
    [ProducesResponseType(typeof(ApiResponse<ProductReviewResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProductReviewResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProductReviewResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ProductReviewResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<ProductReviewResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateProductReview([FromRoute] Guid id, [FromBody] CreateProductReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("⭐ [ProductController] Iniciando criação de avaliação para CreateProductReviewCommand com ProductId {ProductId}", id);

        // Definir o ProductId do comando a partir da rota
        command.ProductId = id;

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<ProductReviewResponse>>(command, cancellationToken);

        _logger.LogInformation("✅ [ProductController] Operação concluída com sucesso para CreateProductReviewCommand com ProductId {ProductId}", id);

        return CreatedAtAction(
            nameof(CreateProductReview),
            new { id = result.Data.Id },
            result);
    }

    /// <summary>
    /// Atualiza uma avaliação de produto existente
    /// </summary>
    /// <param name="productId">ID do produto da avaliação</param>
    /// <param name="reviewId">ID da avaliação a ser atualizada</param>
    /// <param name="command">Dados da atualização da avaliação</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados da avaliação atualizada</returns>
    /// <response code="200">Avaliação atualizada com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Avaliação não encontrada</response>
    /// <response code="403">Usuário não é dono da avaliação</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPut("{productId:guid}/reviews/{reviewId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductReviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProductReview([FromRoute] Guid productId, [FromRoute] Guid reviewId, 
        [FromBody] UpdateProductReviewCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔄 [ProductController] Iniciando atualização de avaliação para UpdateProductReviewCommand com ReviewId {ReviewId} e ProductId {ProductId}", reviewId, productId);

        // Definir o ID da avaliação do comando a partir da rota
        command.Id = reviewId;

        // Validar ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new Error(e.ErrorMessage))
                .ToList();

            throw new ValidationException(errors);
        }

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<ProductReviewResponse>>(command, cancellationToken);

        _logger.LogInformation("✅ [ProductController] Operação concluída com sucesso para UpdateProductReviewCommand com ReviewId {ReviewId} e ProductId {ProductId}", reviewId, productId);

        return Ok(result);
    }

    /// <summary>
    /// Exclui uma avaliação de produto existente (soft delete)
    /// </summary>
    /// <param name="productId">ID do produto da avaliação</param>
    /// <param name="reviewId">ID da avaliação a ser excluída</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Confirmação da exclusão</returns>
    /// <response code="200">Avaliação excluída com sucesso</response>
    /// <response code="404">Avaliação não encontrada</response>
    /// <response code="403">Usuário não é dono da avaliação</response>
    /// <response code="409">Avaliação já foi excluída</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpDelete("{productId:guid}/reviews/{reviewId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteProductReview([FromRoute] Guid productId, [FromRoute] Guid reviewId, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ [ProductController] Iniciando exclusão de avaliação para DeleteProductReviewCommand com ReviewId {ReviewId} e ProductId {ProductId}", reviewId, productId);

        var command = new DeleteProductReviewCommand { Id = reviewId };

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<bool>>(command, cancellationToken);

        _logger.LogInformation("✅ [ProductController] Operação concluída com sucesso para DeleteProductReviewCommand com ReviewId {ReviewId} e ProductId {ProductId}", reviewId, productId);

        return Ok(result);
    }
}