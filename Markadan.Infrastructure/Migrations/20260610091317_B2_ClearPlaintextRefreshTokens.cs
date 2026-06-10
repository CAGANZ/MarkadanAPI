using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markadan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class B2_ClearPlaintextRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RefreshToken.Token artık SHA-256 hash olarak saklanıyor.
            // Eski düz metin kayıtlar hash'lenmiş yeni formatla eşleşmez;
            // kullanıcılar yeniden giriş yaparak yeni token alır.
            migrationBuilder.Sql("DELETE FROM RefreshTokens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Hash geri alınamaz; Down, tabloyu boş bırakır.
        }
    }
}
