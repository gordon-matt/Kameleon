namespace Kameleon;

public static class Constants
{
    /// <summary>
    /// Kameleon usually shares a database with the host, so it keeps its own migration history.
    /// With EF's default table, each side would think the other's migrations are missing.
    /// </summary>
    public const string MigrationsHistoryTable = "__KameleonMigrationsHistory";

    /// <summary>
    /// Prefixed rather than schema-qualified, because SQLite and MySQL have no schemas and one naming
    /// scheme across every provider is easier to find in a shared database.
    /// </summary>
    public static class Tables
    {
        public const string CatalogEntries = "KameleonCatalogEntries";
        public const string TenantSettings = "KameleonTenantSettings";
        public const string TenantThemes = "KameleonTenantThemes";
        public const string UserPreferences = "KameleonUserPreferences";
    }

    public static class MaxLengths
    {
        public const int LayoutId = 64;

        /// <summary>Wide enough for a GUID or a host name, and short enough to lead every composite index.</summary>
        public const int TenantId = 64;

        public const int ThemeId = 64;
        public const int UserId = 128;
    }

    public static class DatabaseProviders
    {
        public const string MySql = "MySql";
        public const string Npgsql = "Npgsql";
        public const string Sqlite = "Sqlite";
        public const string SqlServer = "SqlServer";
    }
}
