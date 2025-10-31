namespace CatalogService.Domain.Enums;

public enum OutboxStatus
{
    Pending,
    Processing,
    Published,
    Failed
}