using Microsoft.AspNetCore.Components;

namespace Kameleon.Catalog;

/// <summary>
/// The rules every theme must pass before it is registered, whatever its source. Theme metadata is treated as
/// untrusted input: a static theme's files can be changed by anyone with access to the folder.
/// </summary>
internal static partial class ThemeValidator
{
    private const int MaxNameLength = 128;
    private const int MaxAssetPathLength = 512;
    private const int MaxChoiceLength = 128;
    private const int MaxTextLength = 4000;

    public static IReadOnlyList<string> Validate(ThemeCandidate candidate, KameleonOptions options)
    {
        var errors = new List<string>();
        var descriptor = candidate.Descriptor;
        bool isCompiled = candidate.Kind == ThemeKind.Compiled;

        if (!ThemeIds.IsValid(descriptor.Id))
        {
            errors.Add($"The id \"{descriptor.Id}\" is not valid. Use lowercase letters, digits and single hyphens, at most {ThemeIds.MaxLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.Name) || descriptor.Name.Length > MaxNameLength)
        {
            errors.Add($"The name is required and can be at most {MaxNameLength} characters.");
        }

        if (!ThemeVersion.TryParse(descriptor.Version, out _))
        {
            errors.Add($"The version \"{descriptor.Version}\" is not a semantic version such as 1.2.0.");
        }

        if (descriptor.PreviewImage is not null && !IsSafeAssetPath(descriptor.PreviewImage, isCompiled))
        {
            errors.Add($"The preview image \"{descriptor.PreviewImage}\" is not a safe relative path.");
        }

        foreach (string stylesheet in descriptor.Stylesheets ?? [])
        {
            if (!IsSafeAssetPath(stylesheet, isCompiled))
            {
                errors.Add($"The stylesheet \"{stylesheet}\" is not a safe relative path.");
            }
        }

        if (!isCompiled && descriptor.Scripts is { Count: > 0 } && !options.AllowStaticThemeScripts)
        {
            errors.Add("Static themes cannot load scripts unless the application allows it.");
        }

        foreach (string script in descriptor.Scripts ?? [])
        {
            if (!IsSafeAssetPath(script, isCompiled))
            {
                errors.Add($"The script \"{script}\" is not a safe relative path.");
            }
        }

        if (isCompiled)
        {
            ValidateLayouts(candidate, errors);
            ValidateViews(candidate, errors);
        }
        else if (candidate.Layouts.Count > 0 || candidate.Views.Count > 0)
        {
            errors.Add("A static theme cannot define layouts or views; those need a compiled theme.");
        }

        ValidateSettings(candidate.Settings, errors);
        return errors;
    }

    /// <summary>
    /// A relative path made of ordinary file name characters, so it cannot leave the theme's folder, name another
    /// scheme or carry markup. A compiled theme may also link an absolute <c>https</c> address.
    /// </summary>
    internal static bool IsSafeAssetPath(string? path, bool allowHttps)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > MaxAssetPathLength)
        {
            return false;
        }

        if (allowHttps && path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && !path.Any(char.IsWhiteSpace);
        }

        if (!AssetPathPattern().IsMatch(path))
        {
            return false;
        }

        return path.Split('/').All(segment => segment.Length > 0 && segment != "." && segment != "..");
    }

    private static void ValidateLayouts(ThemeCandidate candidate, List<string> errors)
    {
        if (candidate.Layouts.Count == 0)
        {
            errors.Add("A compiled theme needs at least one layout.");
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var layout in candidate.Layouts)
        {
            if (!ThemeIds.IsValid(layout.Id))
            {
                errors.Add($"The layout id \"{layout.Id}\" is not valid.");
            }
            else if (!seen.Add(layout.Id))
            {
                errors.Add($"The layout id \"{layout.Id}\" is used more than once.");
            }

            if (string.IsNullOrWhiteSpace(layout.Name) || layout.Name.Length > MaxNameLength)
            {
                errors.Add($"The layout \"{layout.Id}\" needs a name of at most {MaxNameLength} characters.");
            }

            if (layout.ComponentType is null || !typeof(LayoutComponentBase).IsAssignableFrom(layout.ComponentType) || layout.ComponentType.IsAbstract)
            {
                errors.Add($"The layout \"{layout.Id}\" must be a concrete LayoutComponentBase.");
            }
        }

        string? defaultLayoutId = candidate.Descriptor.DefaultLayoutId;
        if (defaultLayoutId is not null && !seen.Contains(defaultLayoutId))
        {
            errors.Add($"The default layout \"{defaultLayoutId}\" is not one of the theme's layouts.");
        }
    }

    private static void ValidateViews(ThemeCandidate candidate, List<string> errors)
    {
        foreach (var (name, type) in candidate.Views)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("A view needs a name.");
            }

            if (type is null || !typeof(IComponent).IsAssignableFrom(type) || type.IsAbstract)
            {
                errors.Add($"The view \"{name}\" must be a concrete Razor component.");
            }
        }
    }

    private static void ValidateSettings(IReadOnlyList<ThemeSettingDefinition> settings, List<string> errors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var setting in settings)
        {
            if (!ThemeIds.IsValid(setting.Key))
            {
                errors.Add($"The setting key \"{setting.Key}\" is not valid.");
                continue;
            }

            if (!seen.Add(setting.Key))
            {
                errors.Add($"The setting key \"{setting.Key}\" is used more than once.");
            }

            if (string.IsNullOrWhiteSpace(setting.Name) || setting.Name.Length > MaxNameLength)
            {
                errors.Add($"The setting \"{setting.Key}\" needs a name of at most {MaxNameLength} characters.");
            }

            if (!Enum.IsDefined(setting.Kind))
            {
                errors.Add($"The setting \"{setting.Key}\" has an unknown kind.");
                continue;
            }

            if (setting.MaxLength is < 1 or > MaxTextLength)
            {
                errors.Add($"The setting \"{setting.Key}\" must allow between 1 and {MaxTextLength} characters.");
            }

            if (setting.Kind == ThemeSettingKind.Choice)
            {
                var choices = setting.Choices ?? [];
                if (choices.Count == 0
                    || choices.Any(choice => string.IsNullOrWhiteSpace(choice) || choice.Length > MaxChoiceLength)
                    || choices.Distinct(StringComparer.Ordinal).Count() != choices.Count)
                {
                    errors.Add($"The setting \"{setting.Key}\" needs distinct, non-empty choices.");
                }
            }

            if (setting.CssVariable is not null
                && (setting.Kind != ThemeSettingKind.Color || !CssVariablePattern().IsMatch(setting.CssVariable)))
            {
                errors.Add($"The setting \"{setting.Key}\" can only set a CSS variable such as --brand-accent, and only for a colour.");
            }

            if (setting.DefaultValue is not null && !ThemeSettingValues.IsValid(setting, setting.DefaultValue, out string? error))
            {
                errors.Add($"The default of \"{setting.Key}\" is not valid: {error}");
            }
        }
    }

    [GeneratedRegex(@"^[A-Za-z0-9._~\-/]+$", RegexOptions.CultureInvariant)]
    private static partial Regex AssetPathPattern();

    [GeneratedRegex("^--[a-zA-Z0-9-]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex CssVariablePattern();
}
