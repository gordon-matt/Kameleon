namespace Kameleon.Tenancy;

/// <summary>
/// The current tenant's key. A multi-tenant host registers its own implementation (scoped is fine) that reads
/// whatever its tenant resolver already decided. Kameleon never resolves tenants itself.
/// </summary>
public interface IThemeTenantProvider
{
    /// <summary>
    /// The current tenant. Empty for a single-tenant site. It must also be correct inside a Blazor circuit, where
    /// there is no <c>HttpContext</c>. Values longer than 64 characters cannot be stored.
    /// </summary>
    string GetTenantId();
}

/// <summary>The default for a single-tenant site: every request belongs to one tenant.</summary>
public sealed class SingleTenantThemeTenantProvider : IThemeTenantProvider
{
    public string GetTenantId() => string.Empty;
}
