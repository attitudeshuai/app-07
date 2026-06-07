using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PointsMall.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashSaleReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReservationCount",
                table: "FlashSales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FlashSaleReminderNotices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MemberUserId = table.Column<int>(type: "int", nullable: false),
                    FlashSaleId = table.Column<int>(type: "int", nullable: false),
                    FlashSaleTitle = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FlashSalePoints = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MinutesBeforeStart = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashSaleReminderNotices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlashSaleReminderNotices_FlashSales_FlashSaleId",
                        column: x => x.FlashSaleId,
                        principalTable: "FlashSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlashSaleReminderNotices_MemberUsers_MemberUserId",
                        column: x => x.MemberUserId,
                        principalTable: "MemberUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FlashSaleReservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FlashSaleId = table.Column<int>(type: "int", nullable: false),
                    MemberUserId = table.Column<int>(type: "int", nullable: false),
                    IsNotified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashSaleReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlashSaleReservations_FlashSales_FlashSaleId",
                        column: x => x.FlashSaleId,
                        principalTable: "FlashSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlashSaleReservations_MemberUsers_MemberUserId",
                        column: x => x.MemberUserId,
                        principalTable: "MemberUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleReminderNotices_CreatedAt",
                table: "FlashSaleReminderNotices",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleReminderNotices_FlashSaleId",
                table: "FlashSaleReminderNotices",
                column: "FlashSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleReminderNotices_IsRead",
                table: "FlashSaleReminderNotices",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleReminderNotices_MemberUserId",
                table: "FlashSaleReminderNotices",
                column: "MemberUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleReservations_FlashSaleId",
                table: "FlashSaleReservations",
                column: "FlashSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleReservations_FlashSaleId_MemberUserId",
                table: "FlashSaleReservations",
                columns: new[] { "FlashSaleId", "MemberUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleReservations_IsNotified",
                table: "FlashSaleReservations",
                column: "IsNotified");

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleReservations_MemberUserId",
                table: "FlashSaleReservations",
                column: "MemberUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlashSaleReminderNotices");

            migrationBuilder.DropTable(
                name: "FlashSaleReservations");

            migrationBuilder.DropColumn(
                name: "ReservationCount",
                table: "FlashSales");
        }
    }
}
