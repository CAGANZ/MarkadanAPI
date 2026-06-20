using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markadan.Infrastructure.Migrations
{
    [DbContext(typeof(MarkadanDbContext))]
    [Migration("20260620010000_G5_TrackingFields")]
    public partial class G5_TrackingFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrackingNumber",
                table: "Carts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingUrl",
                table: "Carts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "TrackingNumber", table: "Carts");
            migrationBuilder.DropColumn(name: "TrackingUrl", table: "Carts");
        }
    }
}
