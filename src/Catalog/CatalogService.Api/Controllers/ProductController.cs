using BuildingBlocks.CQRS.Mediator;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Validations;
using BuildingBlocks.Core.Exceptions;
using CatalogService.Application.Commands.Products.CreateProduct;
using CatalogService.Application.Commands.Products.UpdateProduct;
using CatalogService.Application.Commands.Products.DeleteProduct;
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
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command, CancellationToken cancellationToken = default)
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
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid id, [FromBody] UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➡️ [ProductController] Iniciando atualização para UpdateProductCommand com ID {ProductId}", id);

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

        _logger.LogInformation("✅ [ProductController] Operação concluída com sucesso para UpdateProductCommand com ID {ProductId}", id);
        
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
        _logger.LogInformation("➡️ [ProductController] Iniciando remoção para DeleteProductCommand com ID {ProductId}", id);

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

        _logger.LogInformation("✅ [ProductController] Operação concluída com sucesso para DeleteProductCommand com ID {ProductId}", id);
        
        return Ok(result);
    }
}