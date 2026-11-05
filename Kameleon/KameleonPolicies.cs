namespace Kameleon;

/// <summary>
/// The authorization policies behind Kameleon's screens and services. Each is built from the roles in
/// <see cref="KameleonOptions"/> unless the host registers its own policy with the same name, which is how a
/// host maps them onto permissions it already has.
/// </summary>
public static class KameleonPolicies
{
    /// <summary>
    /// May see the current tenant's theme settings. By default the same as <see cref="Manage"/>; a host can
    /// register it separately for read-only access.
    /// </summary>
    public const string View = "Kameleon.View";

    /// <summary>May change the current tenant's theme settings. Built from <see cref="KameleonOptions.ManagerRoles"/>.</summary>
    public const string Manage = "Kameleon.Manage";

    /// <summary>
    /// May change application-wide theme settings and grant themes to tenants. Built from
    /// <see cref="KameleonOptions.CatalogManagerRoles"/>. In a multi-tenant host this belongs to the platform's
    /// operators, not to tenant administrators.
    /// </summary>
    public const string ManageCatalog = "Kameleon.ManageCatalog";
}
