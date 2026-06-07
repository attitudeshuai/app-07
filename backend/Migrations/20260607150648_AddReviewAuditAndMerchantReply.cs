using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PointsMall.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewAuditAndMerchantReply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "ProductReviews",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MerchantReply",
                table: "ProductReviews",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "MerchantReplyAt",
                table: "ProductReviews",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    PackageNo = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrackingNumber = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShippingCompany = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remark = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShippedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderPackages_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OrderPackageItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrderPackageId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPackageItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderPackageItems_OrderPackages_OrderPackageId",
                        column: x => x.OrderPackageId,
                        principalTable: "OrderPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_IsHidden",
                table: "ProductReviews",
                column: "IsHidden");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackageItems_OrderPackageId",
                table: "OrderPackageItems",
                column: "OrderPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackageItems_ProductId",
                table: "OrderPackageItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackages_OrderId",
                table: "OrderPackages",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackages_PackageNo",
                table: "OrderPackages",
                column: "PackageNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackages_Status",
                table: "OrderPackages",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderPackageItems");

            migrationBuilder.DropTable(
                name: "OrderPackages");

            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_IsHidden",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "MerchantReply",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "MerchantReplyAt",
                table: "ProductReviews");
        }
    }
}
