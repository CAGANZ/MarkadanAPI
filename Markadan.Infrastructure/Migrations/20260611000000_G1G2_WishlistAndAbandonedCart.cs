using System;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markadan.Infrastructure.Migrations
{
    [DbContext(typeof(MarkadanDbContext))]
    [Migration("20260611000000_G1G2_WishlistAndAbandonedCart")]
    public partial class G1G2_WishlistAndAbandonedCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AbandonedReminderSentAt",
                table: "Carts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WishlistItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishlistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WishlistItems_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WishlistItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_AppUserId_ProductId",
                table: "WishlistItems",
                columns: new[] { "AppUserId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_ProductId",
                table: "WishlistItems",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WishlistItems");

            migrationBuilder.DropColumn(
                name: "AbandonedReminderSentAt",
                table: "Carts");
        }
    }
}
