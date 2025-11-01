using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using BuildingBlocks.CQRS.Mediator;

namespace CatalogService.Application.Commands.Categories.CreateCategory;

public class CreateCategoryCommand : ICommand<ApiResponse<CreateCategoryResponse>>
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public string Metadata { get; set; } = "{}";
}