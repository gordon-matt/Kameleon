using System.Collections.ObjectModel;

namespace Kameleon.Settings;

/// <summary>
/// Checks setting values against their definitions. Applied when an administrator saves and again whenever saved
/// values are read, because a theme update can change a definition after values were stored.
/// </summary>
internal static partial class ThemeSettingValues
{
    public const int MaxUrlLength = 1024;

    public static bool IsValid(ThemeSettingDefinition definition, string value, out string? error)
    {
        error = definition.Kind switch
        {
            ThemeSettingKind.Text when value.Length > definition.MaxLength =>
                $"{definition.Name} can be at most {definition.MaxLength} characters.",
            ThemeSettingKind.Text when value.Any(char.IsControl) =>
                $"{definition.Name} cannot contain line breaks or control characters.",
            ThemeSettingKind.Color when !ColorPattern().IsMatch(value) =>
                $"{definition.Name} must be a hex colour such as #0f766e.",
            ThemeSettingKind.Boolean when !bool.TryParse(value, out _) =>
                $"{definition.Name} must be true or false.",
            ThemeSettingKind.Choice when !definition.Choices.Contains(value, StringComparer.Ordinal) =>
                $"{definition.Name} must be one of: {string.Join(", ", definition.Choices)}.",
            ThemeSettingKind.Url when !IsSafeUrl(value) =>
                $"{definition.Name} must be a path starting with / or an http or https address.",
            _ => null,
        };

        return error is null;
    }

    /// <summary>
    /// Validates values an administrator is saving and returns them in stored form. An empty value is dropped,
    /// so the setting goes back to its default.
    /// </summary>
    public static Result<IReadOnlyDictionary<string, string>> Normalize(
        IReadOnlyList<ThemeSettingDefinition> definitions,
        IReadOnlyDictionary<string, string?> values)
    {
        var byKey = definitions.ToDictionary(definition => definition.Key, StringComparer.Ordinal);
        var normalized = new Dictionary<string, string>(StringComparer.Ordinal);
        var errors = new List<ValidationError>();

        foreach (var (key, raw) in values)
        {
            if (!byKey.TryGetValue(key, out var definition))
            {
                errors.Add(new ValidationError(key, "This theme has no setting by that name."));
                continue;
            }

            string value = raw?.Trim() ?? string.Empty;
            if (value.Length == 0)
            {
                continue;
            }

            if (definition.Kind == ThemeSettingKind.Boolean)
            {
                value = value.ToLowerInvariant();
            }

            if (!IsValid(definition, value, out string? error))
            {
                errors.Add(new ValidationError(key, error!));
                continue;
            }

            normalized[key] = value;
        }

        return errors.Count > 0
            ? Result<IReadOnlyDictionary<string, string>>.Invalid(errors)
            : Result<IReadOnlyDictionary<string, string>>.Success(new ReadOnlyDictionary<string, string>(normalized));
    }

    /// <summary>The theme's defaults with valid saved values on top. Unknown keys and invalid values are dropped.</summary>
    public static IReadOnlyDictionary<string, string> Merge(
        IReadOnlyList<ThemeSettingDefinition> definitions,
        IReadOnlyDictionary<string, string>? saved)
    {
        if (definitions.Count == 0)
        {
            return ReadOnlyDictionary<string, string>.Empty;
        }

        var merged = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            if (saved is not null && saved.TryGetValue(definition.Key, out string? value) && IsValid(definition, value, out _))
            {
                merged[definition.Key] = value;
            }
            else if (definition.DefaultValue is not null)
            {
                merged[definition.Key] = definition.DefaultValue;
            }
        }

        return new ReadOnlyDictionary<string, string>(merged);
    }

    public static bool IsColor(string value) => ColorPattern().IsMatch(value);

    private static bool IsSafeUrl(string value)
    {
        if (value.Length > MaxUrlLength || value.Any(character => char.IsControl(character) || char.IsWhiteSpace(character)) || value.Contains('\\'))
        {
            return false;
        }

        if (value.StartsWith('/'))
        {
            return !value.StartsWith("//", StringComparison.Ordinal);
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    [GeneratedRegex("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorPattern();
}
