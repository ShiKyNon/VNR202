using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HanhTrinh.Web.Persistence;

public static class DbSetup
{
    /// <summary>Postgres nếu connection string là URL postgres:// hoặc dạng Host=...; ngược lại SQLite.</summary>
    public static void Configure(DbContextOptionsBuilder o, string? cs)
    {
        cs = string.IsNullOrWhiteSpace(cs) ? "Data Source=hanhtrinh.db" : cs.Trim();
        if (cs.StartsWith("postgres://") || cs.StartsWith("postgresql://")) o.UseNpgsql(FromUrl(cs));
        else if (cs.Contains("Host=", StringComparison.OrdinalIgnoreCase)) o.UseNpgsql(cs);
        else o.UseSqlite(cs);
    }

    /// <summary>Đổi URL kiểu Neon/Render (postgresql://user:pass@host/db?sslmode=require) sang chuỗi Npgsql.</summary>
    public static string FromUrl(string url)
    {
        var u = new Uri(url);
        var userInfo = u.UserInfo.Split(':', 2);
        var b = new NpgsqlConnectionStringBuilder
        {
            Host = u.Host,
            Port = u.IsDefaultPort || u.Port <= 0 ? 5432 : u.Port,
            Database = u.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            SslMode = SslMode.Require,
        };
        return b.ConnectionString;
    }
}
