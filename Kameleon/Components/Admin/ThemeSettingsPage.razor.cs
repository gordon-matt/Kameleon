namespace Kameleon.Components.Admin;

/// <summary>
/// The site's theme settings at the default address. A host that moves the screens renders
/// <see cref="ThemeSettingsScreen"/> from a page with its own route; this one then answers 404.
/// </summary>
public partial class ThemeSettingsPage
{
    private bool isDefaultPath;

    [Inject]
    private IOptions<KameleonOptions> Options { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        isDefaultPath = string.Equals(Options.Value.AdminPath, KameleonOptions.DefaultAdminPath, StringComparison.OrdinalIgnoreCase);
        if (!isDefaultPath)
        {
            Navigation.NotFound();
        }
    }
}
