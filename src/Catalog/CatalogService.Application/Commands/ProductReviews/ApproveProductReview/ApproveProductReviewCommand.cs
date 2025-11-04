using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.ProductReviews.ApproveProductReview;

public class ApproveProductReviewCommand : ICommand<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public Guid ModeratorId { get; set; }
}

