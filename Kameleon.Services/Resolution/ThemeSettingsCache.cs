using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Kameleon.Resolution;

/// <summary>
/// Caches each tenant's settings and each user's choice. Keys always carry the tenant, and user entries also
/// carry the user, so one tenant's or user's result can never be served to another.
/// </summary>
/// <remarks>
/// A tenant's entries are keyed by a version that a change increments, rather than removed. A read that started
/// before the change then stores its stale result under the old version, where nothing looks for it.
/// </remarks>
internal sealed class ThemeSettingsCache(IMemoryCache cache, IOptions<KameleonOptions> options) : IThemeChangeNotifier
{
    private readonly ConcurrentDictionary<string, long> tenantVersions = new(StringComparer.Ordinal);
    private long generation;

    public event EventHandler<ThemeChangedEventArgs>? Changed;

    public async Task<TenantThemeSnapshot> GetSnapshotAsync(string tenantId, IThemeSettingsStore store, CancellationToken cancellationToken)
    {
        var key = new CacheKey(Kind.Tenant, Interlocked.Read(ref generation), tenantId, TenantVersion(tenantId), null);
        if (cache.TryGetValue(key, out TenantThemeSnapshot? cached) && cached is not null)
        {
            return cached;
        }

        var snapshot = await TenantThemeSnapshot.LoadAsync(store, tenantId, cancellationToken);
        Store(key, snapshot);
        return snapshot;
    }

    public async Task<string?> GetUserThemeAsync(string tenantId, string userId, IThemeSettingsStore store, CancellationToken cancellationToken)
    {
        var key = new CacheKey(Kind.User, Interlocked.Read(ref generation), tenantId, TenantVersion(tenantId), userId);
        if (cache.TryGetValue(key, out UserChoice? cached) && cached is not null)
        {
            return cached.ThemeId;
        }

        string? themeId = await store.GetUserThemeAsync(tenantId, userId, cancellationToken);
        Store(key, new UserChoice(themeId));
        return themeId;
    }

    public void TenantChanged(string tenantId)
    {
        tenantVersions.AddOrUpdate(tenantId, 1, (_, version) => version + 1);
        Changed?.Invoke(this, new ThemeChangedEventArgs(tenantId, null));
    }

    public void UserChanged(string tenantId, string userId)
    {
        cache.Remove(new CacheKey(Kind.User, Interlocked.Read(ref generation), tenantId, TenantVersion(tenantId), userId));
        Changed?.Invoke(this, new ThemeChangedEventArgs(tenantId, userId));
    }

    public void AllChanged()
    {
        Interlocked.Increment(ref generation);
        Changed?.Invoke(this, new ThemeChangedEventArgs(null, null));
    }

    private long TenantVersion(string tenantId) => tenantVersions.GetValueOrDefault(tenantId);

    private void Store(CacheKey key, object value)
    {
        var duration = options.Value.CacheDuration;
        if (duration > TimeSpan.Zero)
        {
            cache.Set(key, value, duration);
        }
    }

    private enum Kind
    {
        Tenant,
        User,
    }

    private readonly record struct CacheKey(Kind Kind, long Generation, string TenantId, long TenantVersion, string? UserId);

    /// <summary>Wraps the value so that "no choice" can be cached as well.</summary>
    private sealed record UserChoice(string? ThemeId);
}
