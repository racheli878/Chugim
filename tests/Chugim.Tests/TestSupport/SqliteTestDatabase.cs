using Chugim.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Chugim.Tests.TestSupport;

/// <summary>
/// מסד SQLite אמיתי בזיכרון, עם אותו AppDbContext ואותן הגדרות (כולל ה-concurrency token ונתוני ה-seed).
/// כל עוד החיבור פתוח המסד קיים, וכל DbContext שנוצר כאן הוא "משתמשת" נפרדת מול אותו מסד.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SqliteTestDatabase()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
