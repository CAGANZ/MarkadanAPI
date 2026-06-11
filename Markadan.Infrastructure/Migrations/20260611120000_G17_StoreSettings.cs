using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markadan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class G17_StoreSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreName       = table.Column<string>(type: "nvarchar(100)",  maxLength: 100, nullable: false),
                    LogoUrl         = table.Column<string>(type: "nvarchar(500)",  maxLength: 500, nullable: true),
                    Description     = table.Column<string>(type: "nvarchar(500)",  maxLength: 500, nullable: true),
                    WhatsAppPhone   = table.Column<string>(type: "nvarchar(20)",   maxLength: 20,  nullable: true),
                    ContactPhone    = table.Column<string>(type: "nvarchar(20)",   maxLength: 20,  nullable: true),
                    ContactEmail    = table.Column<string>(type: "nvarchar(200)",  maxLength: 200, nullable: true),
                    InstagramUrl    = table.Column<string>(type: "nvarchar(200)",  maxLength: 200, nullable: true),
                    FacebookUrl     = table.Column<string>(type: "nvarchar(200)",  maxLength: 200, nullable: true),
                    PrimaryColor    = table.Column<string>(type: "nvarchar(7)",    maxLength: 7,   nullable: true),
                    AccentColor     = table.Column<string>(type: "nvarchar(7)",    maxLength: 7,   nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(300)",  maxLength: 300, nullable: true),
                    UpdatedAt       = table.Column<DateTime>(type: "datetime2",    nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StoreSettings");
        }
    }
}
