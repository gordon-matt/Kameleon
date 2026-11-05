using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kameleon.Data;

/// <summary>
/// SQLite and MySQL hand dates back with <see cref="DateTimeKind.Unspecified"/>. Every date is stored
/// in UTC, so it is marked as such on the way out.
/// </summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
{
    public static UtcDateTimeConverter Instance { get; } = new();
}
