namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Normalizes user-supplied <see cref="DateTime"/> values to UTC before persistence.
    /// PostgreSQL 'timestamp with time zone' columns only accept UTC; values deserialized from
    /// date-only JSON (e.g. "2020-03-15") arrive with <see cref="DateTimeKind.Unspecified"/> and
    /// would otherwise be rejected by Npgsql. Date-only values are treated as the same calendar
    /// day in UTC rather than shifted by the server's local offset.
    /// </summary>
    public static class DateTimeNormalizer
    {
        /// <summary>Coerce a value to UTC, returning null when the input is null.</summary>
        public static DateTime? ToUtc(DateTime? value) =>
            value.HasValue ? ToUtc(value.Value) : null;

        /// <summary>Coerce a value to UTC. Unspecified kinds are labelled UTC without shifting.</summary>
        public static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }
}
