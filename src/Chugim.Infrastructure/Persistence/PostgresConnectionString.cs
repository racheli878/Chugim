using Npgsql;

namespace Chugim.Infrastructure.Persistence;

/// <summary>
/// Render (וספקים נוספים) נותנים את כתובת המסד בפורמט URL: postgres://user:pass@host:5432/db.
/// Npgsql מצפה לפורמט Key=Value. כאן מתקבלים שני הפורמטים — ומה שכבר בפורמט הנכון חוזר כמו שהוא.
/// </summary>
public static class PostgresConnectionString
{
    private const int DefaultPort = 5432;

    public static string Normalize(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("postgres" or "postgresql"))
            return value;

        var credentials = uri.UserInfo.Split(':', 2);

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : DefaultPort,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null,
            // הצפנה אם השרת תומך בה (חיבור חיצוני), בלי לדרוש אותה (חיבור פנימי ב-Render).
            SslMode = SslMode.Prefer
        }.ConnectionString;
    }
}
