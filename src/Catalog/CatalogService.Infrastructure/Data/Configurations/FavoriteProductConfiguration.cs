using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Data.Configurations;

public class FavoriteProductConfiguration : IEntityTypeConfiguration<FavoriteProduct>
{
    public void Configure(EntityTypeBuilder<FavoriteProduct> builder)
    {
        // Table mapping
        builder.ToTable("favorite_products");

        // Primary key
        builder.HasKey(fp => fp.Id);
        builder.Property(fp => fp.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        // Properties
        builder.Property(fp => fp.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(fp => fp.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(fp => fp.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        // Relationships
        builder.HasOne<CatalogService.Domain.Aggregates.Product>()
            .WithMany()
            .HasForeignKey(fp => fp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(fp => fp.UserId)
            .HasDatabaseName("idx_favorite_products_user_id");

        builder.HasIndex(fp => fp.ProductId)
            .HasDatabaseName("idx_favorite_products_product_id");

        // Unique constraint
        builder.HasIndex(fp => new { fp.UserId, fp.ProductId })
            .IsUnique();
    }
}