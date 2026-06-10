using Markadan.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Tests.Helpers;

public static class TestDbFactory
{
    /// <summary>
    /// Her test için izole, temiz bir SQLite in-memory DbContext döner.
    /// Connection'ı açık tutmak zorunlu — kapatılırsa in-memory DB silinir.
    /// </summary>
    public static (MarkadanDbContext Db, SqliteConnection Connection) Create()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        // Turkish_100_CI_AI SQL Server'a özgü; SQLite'a case-insensitive Türkçe uyumlu collation kaydediyoruz
        connection.CreateCollation("Turkish_100_CI_AI",
            (x, y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase));

        var options = new DbContextOptionsBuilder<MarkadanDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new MarkadanDbContext(options);
        db.Database.EnsureCreated();

        return (db, connection);
    }
}
