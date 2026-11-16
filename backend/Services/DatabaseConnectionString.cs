using Npgsql;

namespace PensionAgent.Api.Services;

public static class DatabaseConnectionString
{
    public static string Create(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != "postgresql" && uri.Scheme != "postgres"))
        {
            throw new InvalidOperationException("DATABASE_URL must be a PostgreSQL URI.");
        }

        var credentials = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            SslMode = SslMode.Require,
            Pooling = true,
            Timeout = 15,
            CommandTimeout = 30,
            ApplicationName = "pensionAgent"
        };

        foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = parameter.Split('=', 2);
            var key = Uri.UnescapeDataString(pair[0]).ToLowerInvariant();
            var value = pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : string.Empty;

            switch (key)
            {
                case "sslmode":
                    if (!Enum.TryParse<SslMode>(value, true, out var sslMode))
                    {
                        throw new InvalidOperationException("DATABASE_URL contains an unsupported sslmode.");
                    }
                    builder.SslMode = sslMode;
                    break;
                case "connect_timeout":
                    if (!int.TryParse(value, out var timeout) || timeout <= 0)
                    {
                        throw new InvalidOperationException("DATABASE_URL contains an invalid connect_timeout.");
                    }
                    builder.Timeout = timeout;
                    break;
                case "application_name":
                    builder.ApplicationName = value;
                    break;
                case "channel_binding":
                    builder["Channel Binding"] = value;
                    break;
                case "pooling":
                    if (!bool.TryParse(value, out var pooling))
                    {
                        throw new InvalidOperationException("DATABASE_URL contains an invalid pooling value.");
                    }
                    builder.Pooling = pooling;
                    break;
                default:
                    throw new InvalidOperationException($"DATABASE_URL contains an unsupported parameter: {key}.");
            }
        }

        if (string.IsNullOrWhiteSpace(builder.Host) || string.IsNullOrWhiteSpace(builder.Database))
        {
            throw new InvalidOperationException("DATABASE_URL must include a host and database name.");
        }

        return builder.ConnectionString;
    }
}
