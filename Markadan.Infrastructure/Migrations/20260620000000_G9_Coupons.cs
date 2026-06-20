using System;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markadan.Infrastructure.Migrations
{
    [DbContext(typeof(MarkadanDbContext))]
    [Migration("20260620000000_G9_Coupons")]
    public partial class G9_Coupons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    UsageLimit = table.Column<int>(type: "int", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code",
                table: "Coupons",
                column: "Code",
                unique: true);

            migrationBuilder.AddColumn<int>(
                name: "CouponId",
                table: "Carts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CouponCode",
                table: "Carts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Carts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Carts_CouponId",
                table: "Carts",
                column: "CouponId");

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Coupons_CouponId",
                table: "Carts",
                column: "CouponId",
                principalTable: "Coupons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Carts_Coupons_CouponId", table: "Carts");
            migrationBuilder.DropIndex(name: "IX_Carts_CouponId", table: "Carts");
            migrationBuilder.DropColumn(name: "CouponId", table: "Carts");
            migrationBuilder.DropColumn(name: "CouponCode", table: "Carts");
            migrationBuilder.DropColumn(name: "DiscountAmount", table: "Carts");
            migrationBuilder.DropIndex(name: "IX_Coupons_Code", table: "Coupons");
            migrationBuilder.DropTable(name: "Coupons");
        }
    }
}
