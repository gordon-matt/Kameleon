using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Kameleon.Tests.Infrastructure;

/// <summary>The current tenant; tests switch it to act as another site.</summary>
public sealed class TestTenant : IThemeTenantProvider
{
    public string Id { get; set; } = "site-a";

    public string GetTenantId() => Id;
}

/// <summary>The signed-in user, or an anonymous visitor when <see cref="UserId"/> is null.</summary>
public sealed class TestUser : IUserContextService
{
    public string? UserId { get; set; } = "user-1";

    public string? GetCurrentUserId() => UserId;

    public string? GetCurrentUserName() => UserId;

    public bool IsAuthenticated() => UserId is not null;

    public bool IsAdministrator() => false;

    public IReadOnlyList<string> GetRoleNames() => [];
}

public sealed class TestAuthorization : IThemeAuthorization
{
    public bool CanManage { get; set; } = true;

    public bool CanManageCatalog { get; set; } = true;

    public Task<bool> CanViewAsync(CancellationToken cancellationToken = default) => Task.FromResult(CanManage);

    public Task<bool> CanManageAsync(CancellationToken cancellationToken = default) => Task.FromResult(CanManage);

    public Task<bool> CanManageCatalogAsync(CancellationToken cancellationToken = default) => Task.FromResult(CanManageCatalog);
}

public sealed class TestTenantDirectory : IThemeTenantDirectory
{
    public Task<IReadOnlyList<ThemeTenant>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ThemeTenant>>([new("site-a", "Site A"), new("site-b", "Site B")]);
}

public sealed class TestHostEnvironment(string contentRoot) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Testing";

    public string ApplicationName { get; set; } = "Kameleon.Tests";

    public string ContentRootPath { get; set; } = contentRoot;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
