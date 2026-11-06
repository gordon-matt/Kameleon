using Heroicsoft.NetForge.Components.Toasts;

namespace Kameleon.Components;

/// <summary>
/// Lets a signed-in user pick their own theme for the current site. Renders nothing unless the site allows a
/// choice and offers more than one theme. Needs an interactive render mode.
/// </summary>
public partial class ThemePicker
{
    private UserThemeChoicesDto? choices;
    private bool saving;

    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    [Parameter]
    public string Label { get; set; } = "Theme";

    /// <summary>
    /// Gets or sets the class.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    [Inject]
    private IUserThemeService Themes { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ToastService Toasts { get; set; } = default!;

    private string DefaultOptionText
    {
        get
        {
            string? name = choices?.Themes.FirstOrDefault(theme => theme.Id == choices.DefaultThemeId)?.Name;
            return name is null ? "Site default" : $"Site default ({name})";
        }
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        var found = await Themes.GetChoicesAsync();
        choices = found.IsSuccess ? found.Value : null;
    }

    private async Task OnChangeAsync(ChangeEventArgs args)
    {
        string? themeId = args.Value?.ToString();
        saving = true;
        var saved = await Themes.SetAsync(string.IsNullOrEmpty(themeId) ? null : themeId);
        saving = false;

        if (!saved.IsSuccess)
        {
            Toasts.ShowFailure(saved);
            return;
        }

        // A theme can change the document head, which only a full load replaces.
        Navigation.Refresh(forceReload: true);
    }
}
