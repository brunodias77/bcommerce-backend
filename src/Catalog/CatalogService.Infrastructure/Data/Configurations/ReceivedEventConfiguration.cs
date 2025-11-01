using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Data.Configurations;

public class ReceivedEventConfiguration : IEntityTypeConfiguration<ReceivedEvent>
{
    public void Configure(EntityTypeBuilder<ReceivedEvent> builder)
    {
        // Table mapping
        builder.ToTable("received_events");

        // Primary key
        builder.HasKey(re => re.Id);
        builder.Property(re => re.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        // Properties
        builder.Property(re => re.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(re => re.SourceService)
            .HasColumnName("source_service")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(re => re.Payload)
            .HasColumnName("payload")
            .HasColumnType("JSONB")
            .IsRequired();

        builder.Property(re => re.Processed)
            .HasColumnName("processed")
            .HasDefaultValue(false);

        builder.Property(re => re.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(re => re.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        // Indexes
        builder.HasIndex(re => re.EventType)
            .HasDatabaseName("idx_received_events_event_type");

        builder.HasIndex(re => re.SourceService)
            .HasDatabaseName("idx_received_events_source_service");

        builder.HasIndex(re => re.Processed)
            .HasDatabaseName("idx_received_events_processed")
            .HasFilter("processed = false");

        builder.HasIndex(re => re.CreatedAt)
            .HasDatabaseName("idx_received_events_created_at");
    }
}