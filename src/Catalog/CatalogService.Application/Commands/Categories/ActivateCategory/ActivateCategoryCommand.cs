using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.Categories.ActivateCategory;

public class ActivateCategoryCommand : ICommand<ApiResponse<ActivateCategoryResponse>>
{
    public Guid Id { get; set; }
}