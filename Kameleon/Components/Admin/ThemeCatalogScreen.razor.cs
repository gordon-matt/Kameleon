using Heroicsoft.NetForge.Components.Toasts;
using Heroicsoft.NetForge.Results;

namespace Kameleon.Components.Admin;

/// <summary>
/// Application-wide theme settings. Place it on a page guarded by <see cref="KameleonPolicies.ManageCatalog"/>
/// with an interactive render mode; the services check the policy again on every change.
/// </summary>
public partial class ThemeCatalogScreen
{
    private ThemeCatalogDto? data;
    private string? loadError;
    private bool busy;
    private CatalogThemeDto? granting;
    private IReadOnlyList<ThemeTenantGrantDto>? grants;

    [Parameter]
    public string Title { get; set; } = "Theme catalog";

    [Inject]
    private IThemeCatalogAdminService Catalog { get; set; } = default!;

    [Inject]
    private ToastService Toasts { get; set; } = default!;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        var found = await Catalog.GetAsync();
        data = found.IsSuccess ? found.Value : null;
        loadError = found.IsSuccess ? null : found.GetDisplayMessage();
    }

    private Task ReloadAsync() => RunAsync(() => Catalog.ReloadAsync(), "Themes reloaded.");

    private Task SetEnabledAsync(CatalogThemeDto item, bool enabled) =>
        RunAsync(() => Catalog.SetEnabledAsync(item.Theme.Id, enabled), enabled ? $"{item.Theme.Name} is enabled." : $"{item.Theme.Name} is disabled for every site.");

    private Task SetAvailabilityAsync(CatalogThemeDto item, ChangeEventArgs args) =>
        Enum.TryParse(args.Value?.ToString(), out ThemeAvailability availability)
            ? RunAsync(() => Catalog.SetAvailabilityAsync(item.Theme.Id, availability), $"Changed who can use {item.Theme.Name}.")
            : Task.CompletedTask;

    private async Task OpenGrantsAsync(CatalogThemeDto item)
    {
        granting = item;
        grants = null;
        await LoadGrantsAsync();
    }

    private void CloseGrants()
    {
        granting = null;
        grants = null;
    }

    private async Task LoadGrantsAsync()
    {
        if (granting is null)
        {
            return;
        }

        var found = await Catalog.GetGrantsAsync(granting.Theme.Id);
        if (!found.IsSuccess)
        {
            Toasts.ShowFailure(found);
            CloseGrants();
            return;
        }

        grants = found.Value;
    }

    private async Task SetGrantAsync(ThemeTenantGrantDto grant, bool granted)
    {
        if (granting is null)
        {
            return;
        }

        busy = true;
        var saved = await Catalog.SetGrantAsync(granting.Theme.Id, grant.TenantId, granted);
        busy = false;

        if (!saved.IsSuccess)
        {
            Toasts.ShowFailure(saved);
        }

        await LoadGrantsAsync();
    }

    private async Task RunAsync(Func<Task<Result>> change, string success)
    {
        busy = true;
        var result = await change();
        busy = false;

        if (!result.IsSuccess)
        {
            Toasts.ShowFailure(result);
        }
        else
        {
            Toasts.Success(success);
        }

        await LoadAsync();
    }
}
