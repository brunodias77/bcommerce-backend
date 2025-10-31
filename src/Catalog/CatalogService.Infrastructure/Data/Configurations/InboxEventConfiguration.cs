using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Data.Configurations;

public class InboxEventConfiguration : IEntityTypeConfiguration<InboxEvent>
{
    public void Configure(EntityTypeBuilder<InboxEvent> builder)
    {
        // Table mapping
        builder.ToTable("inbox_events");

        // Primary key
        builder.HasKey(ie => ie.Id);
        builder.Property(ie => ie.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        // Properties
        builder.Property(ie => ie.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ie => ie.AggregateId)
            .HasColumnName("aggregate_id")
            .IsRequired();

        builder.Property(ie => ie.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(ie => ie.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        // Indexes
        builder.HasIndex(ie => ie.EventType)
            .HasDatabaseName("idx_inbox_events_event_type");

        builder.HasIndex(ie => ie.AggregateId)
            .HasDatabaseName("idx_inbox_events_aggregate_id");

        builder.HasIndex(ie => ie.CreatedAt)
            .HasDatabaseName("idx_inbox_events_created_at");
    }
}