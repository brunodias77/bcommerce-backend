namespace CatalogService.Application.Commands.ProductReviews.ApproveProductReview;

public class ApproveProductReviewResponse
{
    public Guid ReviewId { get; set; }
    public Guid ModeratorId { get; set; }
    public DateTime ApprovedAt { get; set; }
    public bool IsApproved { get; set; }
}