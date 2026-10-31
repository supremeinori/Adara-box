using System;
using System.Data;
using System.Globalization;
using AdaraBox.Core.Resources;
using Dapper;

namespace AdaraBox.Data.TypeHandlers;

/// <summary>
/// Custom Dapper TypeHandlers for SQLite to ensure proper mapping of Guid, DateTimeOffset, and ResourceType as TEXT.
/// Confirmed and tested in Spike D (Module M00) and Module M03.
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
            SqlMapper.AddTypeHandler(new ResourceTypeHandler());
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

    public sealed class ResourceTypeHandler : SqlMapper.TypeHandler<ResourceType>
    {
        public override void SetValue(IDbDataParameter parameter, ResourceType value)
        {
            parameter.Value = value switch
            {
                ResourceType.Application => "application",
                ResourceType.File => "file",
                ResourceType.Folder => "folder",
                ResourceType.Link => "link",
                _ => "file"
            };
            parameter.DbType = DbType.String;
        }

        public override ResourceType Parse(object value)
        {
            if (value is string s)
            {
                return s.ToLowerInvariant() switch
                {
                    "application" => ResourceType.Application,
                    "file" => ResourceType.File,
                    "folder" => ResourceType.Folder,
                    "link" => ResourceType.Link,
                    _ => ResourceType.File
                };
            }
            return ResourceType.File;
        }
    }
}
