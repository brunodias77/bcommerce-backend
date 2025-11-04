using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.ProductReviews.UpdateProductReview;

public class UpdateProductReviewCommand : ICommand<ApiResponse<ProductReviewResponse>>
{
    public Guid Id { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public bool IsVerifiedPurchase { get; set; }
}