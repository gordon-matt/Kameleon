using System.Text;

namespace Kameleon.Resolution;

/// <summary>
/// Provides theme styles.
/// </summary>
public static class ThemeStyles
{
    /// <summary>
    /// The CSS custom properties a theme's colour settings declare, as one <c>:root</c> rule, or null when there
    /// are none. Only colour values are emitted, and each is checked again here, because the result is written
    /// into a style element without encoding.
    /// </summary>
    public static string? BuildRootVariables(ThemeContext context)
    {
        if (context.Theme is null)
        {
            return null;
        }

        var css = new StringBuilder();
        foreach (var definition in context.Theme.Settings)
        {
            if (definition is { Kind: ThemeSettingKind.Color, CssVariable: { } variable }
                && context.Settings.TryGetValue(definition.Key, out string? value)
                && ThemeSettingValues.IsColor(value))
            {
                css.Append(variable).Append(':').Append(value).Append(';');
            }
        }

        return css.Length == 0 ? null : $":root{{{css}}}";
    }
}
