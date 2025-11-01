using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Infrastructure.Data.Configurations;

public class ReviewVoteConfiguration : IEntityTypeConfiguration<ReviewVote>
{
    public void Configure(EntityTypeBuilder<ReviewVote> builder)
    {
        // Table mapping
        builder.ToTable("review_votes");

        // Primary key
        builder.HasKey(rv => rv.Id);
        builder.Property(rv => rv.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        // Properties
        builder.Property(rv => rv.ReviewId)
            .HasColumnName("review_id")
            .IsRequired();

        builder.Property(rv => rv.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(rv => rv.IsHelpful)
            .HasColumnName("is_helpful")
            .IsRequired();

        builder.Property(rv => rv.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        // Relationships
        builder.HasOne<CatalogService.Domain.Aggregates.ProductReview>()
            .WithMany()
            .HasForeignKey(rv => rv.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(rv => rv.ReviewId)
            .HasDatabaseName("idx_review_votes_review_id");

        builder.HasIndex(rv => rv.UserId)
            .HasDatabaseName("idx_review_votes_user_id");

        // Unique constraint - one vote per user per review
        builder.HasIndex(rv => new { rv.ReviewId, rv.UserId })
            .IsUnique();
    }
}