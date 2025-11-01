using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Data.Context;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    // DbSets for Aggregates
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductReview> ProductReviews { get; set; }

    // DbSets for Entities
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<FavoriteProduct> FavoriteProducts { get; set; }
    public DbSet<ReviewVote> ReviewVotes { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<InboxEvent> InboxEvents { get; set; }
    public DbSet<ReceivedEvent> ReceivedEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new ProductReviewConfiguration());
        modelBuilder.ApplyConfiguration(new ProductImageConfiguration());
        modelBuilder.ApplyConfiguration(new FavoriteProductConfiguration());
        modelBuilder.ApplyConfiguration(new ReviewVoteConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxEventConfiguration());
        modelBuilder.ApplyConfiguration(new InboxEventConfiguration());
        modelBuilder.ApplyConfiguration(new ReceivedEventConfiguration());

        // Enable PostgreSQL extensions
        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.HasPostgresExtension("pg_trgm");
    }
}