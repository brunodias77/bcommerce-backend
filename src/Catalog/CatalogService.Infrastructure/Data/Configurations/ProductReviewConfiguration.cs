using CatalogService.Domain.Aggregates;
using CatalogService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Data.Configurations;

public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        // Table mapping
        builder.ToTable("product_reviews");

        // Primary key
        builder.HasKey(pr => pr.Id);
        builder.Property(pr => pr.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        // Properties
        builder.Property(pr => pr.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(pr => pr.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        // Rating value object
        builder.Property(pr => pr.Rating)
            .HasColumnName("rating")
            .IsRequired()
            .HasConversion(
                rating => rating.Value,
                value => Rating.Create(value)
            );

        builder.Property(pr => pr.Title)
            .HasColumnName("title")
            .HasMaxLength(200);

        builder.Property(pr => pr.Comment)
            .HasColumnName("comment")
            .HasColumnType("TEXT");

        builder.Property(pr => pr.IsVerifiedPurchase)
            .HasColumnName("is_verified_purchase")
            .HasDefaultValue(false);

        builder.Property(pr => pr.HelpfulCount)
            .HasColumnName("helpful_count")
            .HasDefaultValue(0);

        builder.Property(pr => pr.UnhelpfulCount)
            .HasColumnName("unhelpful_count")
            .HasDefaultValue(0);

        // Moderation
        builder.Property(pr => pr.IsApproved)
            .HasColumnName("is_approved")
            .HasDefaultValue(false);

        builder.Property(pr => pr.IsFeatured)
            .HasColumnName("is_featured")
            .HasDefaultValue(false);

        builder.Property(pr => pr.ModeratedAt)
            .HasColumnName("moderated_at");

        builder.Property(pr => pr.ModeratedBy)
            .HasColumnName("moderated_by");

        // Audit fields
        builder.Property(pr => pr.Version)
            .HasColumnName("version")
            .HasDefaultValue(1);

        builder.Property(pr => pr.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(pr => pr.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.Property(pr => pr.DeletedAt)
            .HasColumnName("deleted_at");

        // Relationships
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(pr => pr.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(pr => pr.ProductId)
            .HasDatabaseName("idx_product_reviews_product_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(pr => pr.UserId)
            .HasDatabaseName("idx_product_reviews_user_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(pr => pr.IsApproved)
            .HasDatabaseName("idx_product_reviews_approved")
            .HasFilter("is_approved = TRUE AND deleted_at IS NULL");

        builder.HasIndex(pr => pr.Rating)
            .HasDatabaseName("idx_product_reviews_rating");

        // Soft delete query filter
        builder.HasQueryFilter(pr => pr.DeletedAt == null);
    }
}