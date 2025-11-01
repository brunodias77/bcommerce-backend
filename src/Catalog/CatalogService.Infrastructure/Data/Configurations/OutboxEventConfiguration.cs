using CatalogService.Domain.Entities;
using CatalogService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Data.Configurations;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        // Table mapping
        builder.ToTable("outbox_events");

        // Primary key
        builder.HasKey(oe => oe.Id);
        builder.Property(oe => oe.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        // Properties
        builder.Property(oe => oe.AggregateId)
            .HasColumnName("aggregate_id")
            .IsRequired();

        builder.Property(oe => oe.AggregateType)
            .HasColumnName("aggregate_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(oe => oe.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(oe => oe.EventVersion)
            .HasColumnName("event_version")
            .HasDefaultValue(1);

        builder.Property(oe => oe.Payload)
            .HasColumnName("payload")
            .HasColumnType("JSONB")
            .IsRequired();

        builder.Property(oe => oe.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("JSONB")
            .HasDefaultValue("{}");

        // Enum mapping
        builder.Property(oe => oe.Status)
            .HasColumnName("status")
            .HasConversion(
                status => status.ToString().ToUpper(),
                value => Enum.Parse<OutboxStatus>(value, true)
            )
            .HasDefaultValue(OutboxStatus.Pending);

        builder.Property(oe => oe.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        builder.Property(oe => oe.MaxRetries)
            .HasColumnName("max_retries")
            .HasDefaultValue(3);

        builder.Property(oe => oe.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("TEXT");

        builder.Property(oe => oe.PublishedAt)
            .HasColumnName("published_at");

        builder.Property(oe => oe.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(oe => oe.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        // Indexes
        builder.HasIndex(oe => oe.Status)
            .HasDatabaseName("idx_outbox_events_status")
            .HasFilter("status IN ('PENDING', 'FAILED')");

        builder.HasIndex(oe => oe.CreatedAt)
            .HasDatabaseName("idx_outbox_events_created_at");
    }
}