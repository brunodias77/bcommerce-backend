using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Data.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        // Table mapping
        builder.ToTable("product_images");

        // Primary key
        builder.HasKey(pi => pi.Id);
        builder.Property(pi => pi.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        // Properties
        builder.Property(pi => pi.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(pi => pi.Url)
            .HasColumnName("url")
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(pi => pi.ThumbnailUrl)
            .HasColumnName("thumbnail_url")
            .HasColumnType("TEXT");

        builder.Property(pi => pi.AltText)
            .HasColumnName("alt_text")
            .HasMaxLength(255);

        builder.Property(pi => pi.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0);

        builder.Property(pi => pi.IsPrimary)
            .HasColumnName("is_primary")
            .HasDefaultValue(false);

        builder.Property(pi => pi.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(pi => pi.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        // Relationships
        builder.HasOne<CatalogService.Domain.Aggregates.Product>()
            .WithMany(p => p.Images)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(pi => pi.ProductId)
            .HasDatabaseName("idx_product_images_product_id");

        builder.HasIndex(pi => new { pi.ProductId, pi.IsPrimary })
            .HasDatabaseName("idx_product_images_primary")
            .HasFilter("is_primary = TRUE");
    }
}