using BuildingBlocks.CQRS.Events;
using CatalogService.Domain.Events.Products;

namespace CatalogService.Application.EventHandlers.Products;

public class ProductCreatedEventHandler : IDomainEventHandler<ProductCreatedEvent>
{
    public Task HandleAsync(ProductCreatedEvent notification, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}