using BuildingBlocks.CQRS.Mediator;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Validations;
using CatalogService.Application.Commands.Products.CreateProduct;
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
        try
        {
            _logger.LogInformation("📝 Iniciando criação de produto: {ProductName} com slug: {ProductSlug}", 
                command.Name, command.Slug);

            // Validar ModelState
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("⚠️ Dados inválidos para criação de produto: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                
                
                var errorHandler = new ValidationHandler();
                errorHandler.Add("Dados inválidos.");
                return BadRequest(ApiResponse<CreateProductResponse>.Fail(errorHandler.Errors.ToList()));
            }

            // Enviar command via Mediator
            var result = await _mediator.SendAsync<ApiResponse<CreateProductResponse>>(command, cancellationToken);

            if (result.Success && result.Data != null)
            {
                _logger.LogInformation("✅ Produto criado com sucesso: ID {ProductId}, Nome: {ProductName}", 
                    result.Data.Id, result.Data.Name);
                
                return CreatedAtAction(
                    nameof(CreateProduct), 
                    new { id = result.Data.Id }, 
                    result);
            }

            // Se chegou aqui, houve erro
            _logger.LogWarning("❌ Falha na criação de produto: {ErrorMessage}", result.Message);
            return BadRequest(result);

        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "⚠️ Erro de validação na criação de produto: {ErrorMessage}", ex.Message);
            var errorHandler = new ValidationHandler();
            errorHandler.Add(ex.Message);
            return BadRequest(ApiResponse<CreateProductResponse>.Fail(errorHandler.Errors.ToList()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Erro interno na criação de produto");
            var errorHandler = new ValidationHandler();
            errorHandler.Add("Erro interno do servidor.");
            return StatusCode(
                StatusCodes.Status500InternalServerError, 
                ApiResponse<CreateProductResponse>.Fail(errorHandler.Errors.ToList()));
        }
    }
}