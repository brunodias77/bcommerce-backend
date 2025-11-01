using BuildingBlocks.CQRS.Mediator;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Validations;
using CatalogService.Application.Commands.Categories.CreateCategory;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Api.Controllers;

/// <summary>
/// Controller responsável pelos endpoints de gerenciamento de categorias
/// </summary>
[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(IMediator mediator, ILogger<CategoryController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Cria uma nova categoria
    /// </summary>
    /// <param name="command">Dados da categoria a ser criada</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados da categoria criada</returns>
    /// <response code="201">Categoria criada com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="409">Categoria com slug já existe</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateCategoryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CreateCategoryResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CreateCategoryResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<CreateCategoryResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📝 Iniciando criação de categoria: {CategoryName} com slug: {CategorySlug}", 
                command.Name, command.Slug);

            // Validar ModelState
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("⚠️ Dados inválidos para criação de categoria: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                
                
                var errorHandler = new ValidationHandler();
                errorHandler.Add("Dados inválidos.");
                return BadRequest(ApiResponse<CreateCategoryResponse>.Fail(errorHandler.Errors.ToList()));
            }

            // Enviar command via Mediator
            var result = await _mediator.SendAsync<ApiResponse<CreateCategoryResponse>>(command, cancellationToken);

            if (result.Success && result.Data != null)
            {
                _logger.LogInformation("✅ Categoria criada com sucesso: ID {CategoryId}, Nome: {CategoryName}", 
                    result.Data.Id, result.Data.Name);
                
                return CreatedAtAction(
                    nameof(CreateCategory), 
                    new { id = result.Data.Id }, 
                    result);
            }

            // Se chegou aqui, houve erro
            _logger.LogWarning("❌ Falha na criação de categoria: {ErrorMessage}", result.Message);
            return BadRequest(result);

        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "⚠️ Erro de validação na criação de categoria: {ErrorMessage}", ex.Message);
            var errorHandler = new ValidationHandler();
            errorHandler.Add(ex.Message);
            return BadRequest(ApiResponse<CreateCategoryResponse>.Fail(errorHandler.Errors.ToList()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Erro interno na criação de categoria");
            var errorHandler = new ValidationHandler();
            errorHandler.Add("Erro interno do servidor.");
            return StatusCode(
                StatusCodes.Status500InternalServerError, 
                ApiResponse<CreateCategoryResponse>.Fail(errorHandler.Errors.ToList()));
        }
    }
}