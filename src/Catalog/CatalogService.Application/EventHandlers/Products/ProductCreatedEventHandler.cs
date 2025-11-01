using BuildingBlocks.CQRS.Events;
using CatalogService.Application.Events.Products;

namespace CatalogService.Application.EventHandlers.Products;

public class ProductCreatedEventHandler : IDomainEventHandler<ProductCreatedNotification>
{
    public Task HandleAsync(ProductCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}