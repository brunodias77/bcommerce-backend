using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.ProductReviews.DeleteProductReview;

public class DeleteProductReviewCommand : ICommand<ApiResponse<bool>>
{
    public Guid Id { get; set; }
}