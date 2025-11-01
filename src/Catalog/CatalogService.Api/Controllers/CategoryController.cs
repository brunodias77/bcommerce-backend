using BuildingBlocks.CQRS.Mediator;
using BuildingBlocks.Core.Responses;
using BuildingBlocks.Core.Validations;
using BuildingBlocks.Core.Exceptions;
using CatalogService.Application.Commands.Categories.CreateCategory;
using CatalogService.Application.Commands.Categories.UpdateCategory;
using CatalogService.Application.Commands.Categories.DeleteCategory;
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
        _logger.LogInformation("📝 Iniciando criação de categoria: {CategoryName} com slug: {CategorySlug}", 
            command.Name, command.Slug);

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
        var result = await _mediator.SendAsync<ApiResponse<CreateCategoryResponse>>(command, cancellationToken);

        _logger.LogInformation("✅ Categoria criada com sucesso: ID {CategoryId}, Nome: {CategoryName}", 
            result.Data.Id, result.Data.Name);
        
        return CreatedAtAction(
            nameof(CreateCategory), 
            new { id = result.Data.Id }, 
            result);
    }

    /// <summary>
    /// Atualiza uma categoria existente
    /// </summary>
    /// <param name="id">ID da categoria a ser atualizada</param>
    /// <param name="command">Dados da categoria a ser atualizada</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados da categoria atualizada</returns>
    /// <response code="200">Categoria atualizada com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Categoria não encontrada</response>
    /// <response code="409">Categoria com slug já existe</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateCategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UpdateCategoryResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UpdateCategoryResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UpdateCategoryResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<UpdateCategoryResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCategory([FromRoute] Guid id, [FromBody] UpdateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔄 Iniciando atualização de categoria: ID {CategoryId}, Nome: {CategoryName}", 
            id, command.Name);

        // Garantir que o ID da rota seja usado no comando
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
        var result = await _mediator.SendAsync<ApiResponse<UpdateCategoryResponse>>(command, cancellationToken);

        _logger.LogInformation("✅ Categoria atualizada com sucesso: ID {CategoryId}, Nome: {CategoryName}", 
            result.Data.Id, result.Data.Name);
        
        return Ok(result);
    }

    /// <summary>
    /// Deleta uma categoria existente (soft delete)
    /// </summary>
    /// <param name="id">ID da categoria a ser deletada</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Confirmação da exclusão</returns>
    /// <response code="200">Categoria deletada com sucesso</response>
    /// <response code="400">Dados inválidos ou erro de validação</response>
    /// <response code="404">Categoria não encontrada</response>
    /// <response code="409">Categoria possui dependências (subcategorias ou produtos)</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteCategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DeleteCategoryResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DeleteCategoryResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<DeleteCategoryResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<DeleteCategoryResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCategory([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ Iniciando exclusão de categoria: ID {CategoryId}", id);

        // Criar o comando
        var command = new DeleteCategoryCommand(id);

        // Enviar command via Mediator
        var result = await _mediator.SendAsync<ApiResponse<DeleteCategoryResponse>>(command, cancellationToken);

        _logger.LogInformation("✅ Categoria deletada com sucesso: ID {CategoryId}", id);
        
        return Ok(result);
    }
}