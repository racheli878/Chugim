using Chugim.Infrastructure.Persistence;
using Npgsql;

namespace Chugim.Tests.Infrastructure;

public class PostgresConnectionStringTests
{
    [Fact]
    public void Url_FromHostingProvider_IsConvertedToNpgsqlFormat()
    {
        // סיסמה עם תו מקודד (%40 = @) — חייבת להגיע מפוענחת.
        var url = "postgres://chugim_user:p%40ss@dpg-abc123.frankfurt-postgres.render.com/chugim_db";

        var builder = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize(url));

        Assert.Equal("dpg-abc123.frankfurt-postgres.render.com", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("chugim_db", builder.Database);
        Assert.Equal("chugim_user", builder.Username);
        Assert.Equal("p@ss", builder.Password);
    }

    [Fact]
    public void KeyValueConnectionString_IsReturnedUnchanged()
    {
        const string connectionString = "Host=localhost;Port=5432;Database=appdb;Username=postgres;Password=secret";

        Assert.Equal(connectionString, PostgresConnectionString.Normalize(connectionString));
    }
}
