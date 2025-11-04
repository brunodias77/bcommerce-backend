using BuildingBlocks.Core.Events;
using CatalogService.Domain.Enums;

namespace CatalogService.Domain.Events.Products;

public record ProductStockUpdatedEvent(
    Guid ProductId,
    string Name,
    int PreviousStock,
    int NewStock,
    StockOperation Operation,
    DateTime UpdatedAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}