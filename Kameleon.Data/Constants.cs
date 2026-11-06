namespace Kameleon;

/// <summary>
/// Provides constants.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Kameleon usually shares a database with the host, so it keeps its own migration history.
    /// With EF's default table, each side would think the other's migrations are missing.
    /// </summary>
    public const string MigrationsHistoryTable = "__KameleonMigrationsHistory";

    /// <summary>Database schema for providers that support one (SQL Server and PostgreSQL).</summary>
    public const string Schema = "kmln";

    /// <summary>
    /// Prepended to every table name on providers without schemas. In MySQL a schema is a database,
    /// and SQLite has none, so the tables stay unqualified and are prefixed instead.
    /// </summary>
    public const string TablePrefix = "Kameleon_";

    /// <summary>
    /// Unqualified table names. The schema or <see cref="TablePrefix"/> is applied by the context.
    /// </summary>
    public static class Tables
    {
        /// <summary>
        /// The catalog entries constant.
        /// </summary>
        public const string CatalogEntries = "CatalogEntries";
        /// <summary>
        /// The tenant settings constant.
        /// </summary>
        public const string TenantSettings = "TenantSettings";
        /// <summary>
        /// The tenant themes constant.
        /// </summary>
        public const string TenantThemes = "TenantThemes";
        /// <summary>
        /// The user preferences constant.
        /// </summary>
        public const string UserPreferences = "UserPreferences";
    }

    /// <summary>
    /// Provides max lengths.
    /// </summary>
    public static class MaxLengths
    {
        /// <summary>
        /// The layout identifier constant.
        /// </summary>
        public const int LayoutId = 64;

        /// <summary>Wide enough for a GUID or a host name, and short enough to lead every composite index.</summary>
        public const int TenantId = 64;

        /// <summary>
        /// The theme identifier constant.
        /// </summary>
        public const int ThemeId = 64;
        /// <summary>
        /// The user identifier constant.
        /// </summary>
        public const int UserId = 128;
    }

    /// <summary>
    /// Provides database providers.
    /// </summary>
    public static class DatabaseProviders
    {
        /// <summary>
        /// The my SQL constant.
        /// </summary>
        public const string MySql = "MySql";
        /// <summary>
        /// The npgsql constant.
        /// </summary>
        public const string Npgsql = "Npgsql";
        /// <summary>
        /// The sqlite constant.
        /// </summary>
        public const string Sqlite = "Sqlite";
        /// <summary>
        /// The SQL server constant.
        /// </summary>
        public const string SqlServer = "SqlServer";
    }
}
