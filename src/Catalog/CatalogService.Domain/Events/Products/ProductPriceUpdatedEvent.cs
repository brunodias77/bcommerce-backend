using BuildingBlocks.Core.Events;
using CatalogService.Domain.ValueObjects;

namespace CatalogService.Domain.Events.Products;

public record ProductPriceUpdatedEvent(
    Guid ProductId,
    string Name,
    Money? PreviousPrice,
    Money NewPrice,
    Money? PreviousCompareAtPrice,
    Money? NewCompareAtPrice,
    DateTime UpdatedAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}