using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using BuildingBlocks.CQRS.Mediator;

namespace CatalogService.Application.Commands.Categories.CreateCategory;

public class CreateCategoryCommand : ICommand<ApiResponse<CreateCategoryResponse>>
{
    
}