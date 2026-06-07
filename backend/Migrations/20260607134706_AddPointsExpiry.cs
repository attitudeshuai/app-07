using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PointsMall.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvailablePoints",
                table: "PointsRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpireAt",
                table: "PointsRecords",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExpired",
                table: "PointsRecords",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAt",
                table: "Orders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PointsExpiryNotices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MemberUserId = table.Column<int>(type: "int", nullable: false),
                    PointsExpiring = table.Column<int>(type: "int", nullable: false),
                    ExpireDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DaysBeforeExpiry = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsExpiryNotices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointsExpiryNotices_MemberUsers_MemberUserId",
                        column: x => x.MemberUserId,
                        principalTable: "MemberUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PointsRecords_ExpireAt",
                table: "PointsRecords",
                column: "ExpireAt");

            migrationBuilder.CreateIndex(
                name: "IX_PointsRecords_IsExpired",
                table: "PointsRecords",
                column: "IsExpired");

            migrationBuilder.CreateIndex(
                name: "IX_PointsRecords_MemberUserId_IsExpired_ExpireAt",
                table: "PointsRecords",
                columns: new[] { "MemberUserId", "IsExpired", "ExpireAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PointsExpiryNotices_ExpireDate",
                table: "PointsExpiryNotices",
                column: "ExpireDate");

            migrationBuilder.CreateIndex(
                name: "IX_PointsExpiryNotices_IsRead",
                table: "PointsExpiryNotices",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_PointsExpiryNotices_MemberUserId",
                table: "PointsExpiryNotices",
                column: "MemberUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointsExpiryNotices");

            migrationBuilder.DropIndex(
                name: "IX_PointsRecords_ExpireAt",
                table: "PointsRecords");

            migrationBuilder.DropIndex(
                name: "IX_PointsRecords_IsExpired",
                table: "PointsRecords");

            migrationBuilder.DropIndex(
                name: "IX_PointsRecords_MemberUserId_IsExpired_ExpireAt",
                table: "PointsRecords");

            migrationBuilder.DropColumn(
                name: "AvailablePoints",
                table: "PointsRecords");

            migrationBuilder.DropColumn(
                name: "ExpireAt",
                table: "PointsRecords");

            migrationBuilder.DropColumn(
                name: "IsExpired",
                table: "PointsRecords");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "Orders");
        }
    }
}
