using CatalogService.Domain.Aggregates;
using CatalogService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Table mapping
        builder.ToTable("products");

        // Primary key
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        // Basic properties
        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasColumnName("slug")
            .HasMaxLength(180)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasColumnType("TEXT");

        builder.Property(p => p.ShortDescription)
            .HasColumnName("short_description")
            .HasMaxLength(500);

        // Money value objects - Price
        builder.Property(p => p.Price)
            .HasColumnName("price")
            .HasColumnType("DECIMAL(10,2)")
            .IsRequired()
            .HasConversion(
                money => money.Amount,
                amount => Money.Create(amount, "BRL")
            );

        // Money value objects - CompareAtPrice
        builder.Property(p => p.CompareAtPrice)
            .HasColumnName("compare_at_price")
            .HasColumnType("DECIMAL(10,2)")
            .HasConversion(
                money => money != null ? money.Amount : (decimal?)null,
                amount => amount.HasValue ? Money.Create(amount.Value, "BRL") : null
            );

        // Money value objects - CostPrice
        builder.Property(p => p.CostPrice)
            .HasColumnName("cost_price")
            .HasColumnType("DECIMAL(10,2)")
            .HasConversion(
                money => money != null ? money.Amount : (decimal?)null,
                amount => amount.HasValue ? Money.Create(amount.Value, "BRL") : null
            );

        // Inventory
        builder.Property(p => p.Stock)
            .HasColumnName("stock")
            .HasDefaultValue(0);

        builder.Property(p => p.StockReserved)
            .HasColumnName("stock_reserved")
            .HasDefaultValue(0);

        builder.Property(p => p.LowStockThreshold)
            .HasColumnName("low_stock_threshold")
            .HasDefaultValue(10);

        // Categorization
        builder.Property(p => p.CategoryId)
            .HasColumnName("category_id");

        // SEO
        builder.Property(p => p.MetaTitle)
            .HasColumnName("meta_title")
            .HasMaxLength(200);

        builder.Property(p => p.MetaDescription)
            .HasColumnName("meta_description")
            .HasMaxLength(500);

        // Attributes
        builder.Property(p => p.WeightKg)
            .HasColumnName("weight_kg")
            .HasColumnType("DECIMAL(10,3)");

        // Dimensions value object - stored as string in database
        builder.Property(p => p.DimensionsCm)
            .HasColumnName("dimensions_cm")
            .HasMaxLength(50)
            .HasConversion(
                d => d != null ? d.ToString() : null,
                s => Dimensions.FromString(s)
            );

        builder.Property(p => p.Sku)
            .HasColumnName("sku")
            .HasMaxLength(100);

        builder.Property(p => p.Barcode)
            .HasColumnName("barcode")
            .HasMaxLength(100);

        // Status
        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(p => p.IsFeatured)
            .HasColumnName("is_featured")
            .HasDefaultValue(false);

        // Stats (denormalized)
        builder.Property(p => p.ViewCount)
            .HasColumnName("view_count")
            .HasDefaultValue(0);

        builder.Property(p => p.FavoriteCount)
            .HasColumnName("favorite_count")
            .HasDefaultValue(0);

        builder.Property(p => p.ReviewCount)
            .HasColumnName("review_count")
            .HasDefaultValue(0);

        builder.Property(p => p.ReviewAvgRating)
            .HasColumnName("review_avg_rating")
            .HasColumnType("DECIMAL(3,2)")
            .HasDefaultValue(0);

        // Audit fields
        builder.Property(p => p.Version)
            .HasColumnName("version")
            .HasDefaultValue(1);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at");

        // Relationships
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Ignore computed properties
        builder.Ignore(p => p.AvailableStock);

        // Indexes
        builder.HasIndex(p => p.Slug)
            .IsUnique()
            .HasDatabaseName("idx_products_slug")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(p => p.CategoryId)
            .HasDatabaseName("idx_products_category_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("idx_products_active")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(p => p.IsFeatured)
            .HasDatabaseName("idx_products_featured")
            .HasFilter("is_featured = TRUE AND deleted_at IS NULL");

        builder.HasIndex(p => p.Sku)
            .IsUnique()
            .HasDatabaseName("idx_products_sku")
            .HasFilter("deleted_at IS NULL");

        // Additional indexes from schema
        builder.HasIndex(p => p.Price)
            .HasDatabaseName("idx_products_price")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(p => p.Stock)
            .HasDatabaseName("idx_products_stock")
            .HasFilter("deleted_at IS NULL");

        // Full-text search indexes (GIN)
        builder.HasIndex(p => p.Name)
            .HasDatabaseName("idx_products_name_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(p => p.Description)
            .HasDatabaseName("idx_products_description_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        // Soft delete query filter
        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}