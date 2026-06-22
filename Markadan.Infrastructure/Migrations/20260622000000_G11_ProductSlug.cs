using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markadan.Infrastructure.Migrations
{
    [DbContext(typeof(MarkadanDbContext))]
    [Migration("20260622000000_G11_ProductSlug")]
    public partial class G11_ProductSlug : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nullable olarak ekle — mevcut satırlar için varsayılan değer atanacak
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                collation: "Turkish_100_CI_AI");

            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Mevcut ürünler için slug = id (benzersizliği garantiler; admin daha sonra düzenler)
            migrationBuilder.Sql("UPDATE Products SET Slug = CAST(Id AS nvarchar(20))");

            // NOT NULL yap
            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                collation: "Turkish_100_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldCollation: "Turkish_100_CI_AI");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Products_Slug", table: "Products");
            migrationBuilder.DropColumn(name: "Slug", table: "Products");
            migrationBuilder.DropColumn(name: "MetaDescription", table: "Products");
        }
    }
}
