using BuildingBlocks.Core.Events;

namespace CatalogService.Domain.Events.Products;

public record ProductFeaturedEvent(
    Guid ProductId,
    string Name,
    DateTime FeaturedAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}