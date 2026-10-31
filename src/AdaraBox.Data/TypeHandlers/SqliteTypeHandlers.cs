using System;
using System.Data;
using System.Globalization;
using Dapper;

namespace AdaraBox.Data.TypeHandlers;

/// <summary>
/// Custom Dapper TypeHandlers for SQLite to ensure proper mapping of Guid and DateTimeOffset as TEXT.
/// Confirmed and tested in Spike D (Module M00).
/// </summary>
public static class SqliteTypeHandlers
{
    private static bool _isRegistered = false;
    private static readonly object _lock = new();

    public static void Register()
    {
        if (_isRegistered) return;

        lock (_lock)
        {
            if (_isRegistered) return;

            SqlMapper.AddTypeHandler(new GuidTypeHandler());
            SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
            _isRegistered = true;
        }
    }

    public sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            // Consistent with Microsoft.Data.Sqlite default uppercase Guid representation
            parameter.Value = value.ToString("D").ToUpperInvariant();
            parameter.DbType = DbType.String;
        }

        public override Guid Parse(object value)
        {
            return value switch
            {
                string s when Guid.TryParse(s, out var guid) => guid,
                byte[] bytes when bytes.Length == 16 => new Guid(bytes),
                _ => Guid.Empty
            };
        }
    }

    public sealed class DateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            parameter.Value = value.ToString("O", CultureInfo.InvariantCulture);
            parameter.DbType = DbType.String;
        }

        public override DateTimeOffset Parse(object value)
        {
            if (value is string s && DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
            {
                return dto;
            }

            if (value is DateTime dt)
            {
                return new DateTimeOffset(dt);
            }

            return DateTimeOffset.MinValue;
        }
    }
}
