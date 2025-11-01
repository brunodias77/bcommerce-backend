using CatalogService.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // Table mapping
        builder.ToTable("categories");

        // Primary key
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        // Properties
        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Slug)
            .HasColumnName("slug")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasColumnType("TEXT");

        builder.Property(c => c.ParentId)
            .HasColumnName("parent_id");

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(c => c.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0);

        builder.Property(c => c.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("JSONB")
            .HasDefaultValue("{}");

        builder.Property(c => c.Version)
            .HasColumnName("version")
            .HasDefaultValue(1);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        // Indexes
        builder.HasIndex(c => c.Slug)
            .HasDatabaseName("idx_categories_slug");

        builder.HasIndex(c => c.ParentId)
            .HasDatabaseName("idx_categories_parent_id");

        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("idx_categories_active");

        // Unique constraints
        builder.HasIndex(c => c.Slug)
            .IsUnique();

        // Self-referencing relationship
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}