using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    metadata = table.Column<string>(type: "JSONB", nullable: false, defaultValue: "{}"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_categories_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "inbox_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    payload = table.Column<string>(type: "JSONB", nullable: false),
                    metadata = table.Column<string>(type: "JSONB", nullable: false, defaultValue: "{}"),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "PENDING"),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_retries = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    error_message = table.Column<string>(type: "TEXT", nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "received_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_service = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "JSONB", nullable: false),
                    processed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_received_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    short_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    compare_at_price = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    cost_price = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    stock = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    stock_reserved = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    low_stock_threshold = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    meta_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    meta_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    weight_kg = table.Column<decimal>(type: "numeric(10,3)", nullable: true),
                    dimensions_cm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    favorite_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    review_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    review_avg_rating = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0m),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                    table.ForeignKey(
                        name: "FK_products_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "favorite_products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_favorite_products", x => x.id);
                    table.ForeignKey(
                        name: "FK_favorite_products_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "TEXT", nullable: false),
                    thumbnail_url = table.Column<string>(type: "TEXT", nullable: true),
                    alt_text = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_images_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    comment = table.Column<string>(type: "TEXT", nullable: true),
                    is_verified_purchase = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    helpful_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    unhelpful_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    moderated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    moderated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_reviews_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_votes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_helpful = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_votes", x => x.id);
                    table.ForeignKey(
                        name: "FK_review_votes_product_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "product_reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_categories_active",
                table: "categories",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_categories_parent_id",
                table: "categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "idx_categories_slug",
                table: "categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_favorite_products_product_id",
                table: "favorite_products",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "idx_favorite_products_user_id",
                table: "favorite_products",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_favorite_products_user_id_product_id",
                table: "favorite_products",
                columns: new[] { "user_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_inbox_events_aggregate_id",
                table: "inbox_events",
                column: "aggregate_id");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_events_created_at",
                table: "inbox_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_inbox_events_event_type",
                table: "inbox_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_events_created_at",
                table: "outbox_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_events_status",
                table: "outbox_events",
                column: "status",
                filter: "status IN ('PENDING', 'FAILED')");

            migrationBuilder.CreateIndex(
                name: "idx_product_images_primary",
                table: "product_images",
                columns: new[] { "product_id", "is_primary" },
                filter: "is_primary = TRUE");

            migrationBuilder.CreateIndex(
                name: "idx_product_images_product_id",
                table: "product_images",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "idx_product_reviews_approved",
                table: "product_reviews",
                column: "is_approved",
                filter: "is_approved = TRUE AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_product_reviews_product_id",
                table: "product_reviews",
                column: "product_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_product_reviews_rating",
                table: "product_reviews",
                column: "rating");

            migrationBuilder.CreateIndex(
                name: "idx_product_reviews_user_id",
                table: "product_reviews",
                column: "user_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_products_active",
                table: "products",
                column: "is_active",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_products_category_id",
                table: "products",
                column: "category_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_products_description_trgm",
                table: "products",
                column: "description")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "idx_products_featured",
                table: "products",
                column: "is_featured",
                filter: "is_featured = TRUE AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_products_name_trgm",
                table: "products",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "idx_products_price",
                table: "products",
                column: "price",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_products_sku",
                table: "products",
                column: "sku",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_products_slug",
                table: "products",
                column: "slug",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_products_stock",
                table: "products",
                column: "stock",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_received_events_created_at",
                table: "received_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_received_events_event_type",
                table: "received_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "idx_received_events_processed",
                table: "received_events",
                column: "processed",
                filter: "processed = false");

            migrationBuilder.CreateIndex(
                name: "idx_received_events_source_service",
                table: "received_events",
                column: "source_service");

            migrationBuilder.CreateIndex(
                name: "idx_review_votes_review_id",
                table: "review_votes",
                column: "review_id");

            migrationBuilder.CreateIndex(
                name: "idx_review_votes_user_id",
                table: "review_votes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_review_votes_review_id_user_id",
                table: "review_votes",
                columns: new[] { "review_id", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "favorite_products");

            migrationBuilder.DropTable(
                name: "inbox_events");

            migrationBuilder.DropTable(
                name: "outbox_events");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "received_events");

            migrationBuilder.DropTable(
                name: "review_votes");

            migrationBuilder.DropTable(
                name: "product_reviews");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
