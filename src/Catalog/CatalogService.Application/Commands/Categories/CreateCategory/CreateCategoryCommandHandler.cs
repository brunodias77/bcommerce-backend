using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.Categories.CreateCategory;

public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, ApiResponse<CreateCategoryResponse>>
{
    public Task<ApiResponse<CreateCategoryResponse>> HandleAsync(CreateCategoryCommand request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}