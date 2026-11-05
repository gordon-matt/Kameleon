using Heroicsoft.NetForge.Components.Toasts;
using Heroicsoft.NetForge.Results;

namespace Kameleon.Components.Admin;

/// <summary>
/// The current tenant's theme settings. Place it on a page guarded by <see cref="KameleonPolicies.Manage"/>
/// with an interactive render mode; the services check the policy again on every change.
/// </summary>
public partial class ThemeSettingsScreen
{
    private TenantThemesDto? data;
    private string? loadError;
    private bool busy;
    private TenantThemeDto? editing;
    private Dictionary<string, string?> values = new(StringComparer.Ordinal);
    private IReadOnlyList<ValidationError> errors = [];

    [Parameter]
    public string Title { get; set; } = "Themes";

    [Inject]
    private IThemeAdminService Themes { get; set; } = default!;

    [Inject]
    private ToastService Toasts { get; set; } = default!;

    private string? FallbackName =>
        data?.FallbackThemeId is { } id ? data.Themes.FirstOrDefault(theme => theme.Theme.Id == id)?.Theme.Name ?? id : null;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        var found = await Themes.GetAsync();
        data = found.IsSuccess ? found.Value : null;
        loadError = found.IsSuccess ? null : found.GetDisplayMessage();
    }

    private Task SetAllowUserOverrideAsync(bool allow) =>
        RunAsync(() => Themes.SetAllowUserOverrideAsync(allow), allow ? "Users can now choose their own theme." : "Everyone now sees the default theme.");

    private Task SetEnabledAsync(TenantThemeDto item, bool enabled) =>
        RunAsync(() => Themes.SetEnabledAsync(item.Theme.Id, enabled), enabled ? $"{item.Theme.Name} is now offered." : $"{item.Theme.Name} is no longer offered.");

    private Task SetDefaultAsync(TenantThemeDto item) =>
        RunAsync(() => Themes.SetDefaultAsync(new SetDefaultThemeRequest { ThemeId = item.Theme.Id }), $"{item.Theme.Name} is now the default theme.");

    private Task SetLayoutAsync(TenantThemeDto item, ChangeEventArgs args)
    {
        string? layoutId = args.Value?.ToString();
        return RunAsync(
            () => Themes.SetDefaultAsync(new SetDefaultThemeRequest { ThemeId = item.Theme.Id, LayoutId = string.IsNullOrEmpty(layoutId) ? null : layoutId }),
            "Layout changed.");
    }

    private void OpenSettings(TenantThemeDto item)
    {
        editing = item;
        errors = [];
        values = item.SettingDefinitions.ToDictionary(
            definition => definition.Key,
            definition => item.Settings.GetValueOrDefault(definition.Key),
            StringComparer.Ordinal);
    }

    private void CloseSettings() => editing = null;

    private void ResetSettings()
    {
        foreach (string key in values.Keys)
        {
            values[key] = null;
        }

        errors = [];
    }

    private async Task SaveSettingsAsync()
    {
        if (editing is null)
        {
            return;
        }

        busy = true;
        var saved = await Themes.SaveSettingsAsync(new SaveThemeSettingsRequest { ThemeId = editing.Theme.Id, Values = values });
        busy = false;

        if (saved.Status == ResultStatus.Invalid)
        {
            errors = [.. saved.ValidationErrors];
            return;
        }

        if (!saved.IsSuccess)
        {
            Toasts.ShowFailure(saved);
            return;
        }

        Toasts.Success($"Saved the settings of {editing.Theme.Name}.");
        editing = null;
        await LoadAsync();
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

    private string ValueOf(ThemeSettingDefinition definition) =>
        values.GetValueOrDefault(definition.Key) ?? definition.DefaultValue ?? string.Empty;

    private bool IsOn(ThemeSettingDefinition definition) =>
        string.Equals(ValueOf(definition), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>A colour input only accepts six-digit hex, so other forms show black until they are changed.</summary>
    private string SwatchValue(ThemeSettingDefinition definition)
    {
        string value = ValueOf(definition);
        return value.Length == 7 && value[0] == '#' ? value : "#000000";
    }

    private void SetValue(ThemeSettingDefinition definition, string? value)
    {
        values[definition.Key] = value;
        errors = [.. errors.Where(error => !string.Equals(error.Identifier, definition.Key, StringComparison.Ordinal))];
    }

    private void SetFlag(ThemeSettingDefinition definition, bool on) => SetValue(definition, on ? "true" : "false");

    private string? ErrorFor(string key) =>
        errors.FirstOrDefault(error => string.Equals(error.Identifier, key, StringComparison.Ordinal))?.ErrorMessage;

    private static string DefaultLayoutName(TenantThemeDto item) =>
        item.Theme.Layouts.FirstOrDefault(layout => layout.Id == item.Theme.DefaultLayoutId)?.Name ?? item.Theme.DefaultLayoutId ?? "none";
}
