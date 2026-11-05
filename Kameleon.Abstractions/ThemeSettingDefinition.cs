namespace Kameleon;

/// <summary>
/// A value an administrator can set for a theme in each tenant. Saved values are validated against this
/// definition on save and again on load, so a theme only ever sees values of the declared kind.
/// </summary>
public sealed record ThemeSettingDefinition
{
    /// <summary>Lowercase letters, digits and single hyphens. Read it back with <see cref="ThemeContext.Setting"/>.</summary>
    public required string Key { get; init; }

    public required string Name { get; init; }

    public ThemeSettingKind Kind { get; init; } = ThemeSettingKind.Text;

    /// <summary>Used until an administrator saves a value. Must itself be valid for <see cref="Kind"/>.</summary>
    public string? DefaultValue { get; init; }

    public string? Description { get; init; }

    /// <summary>The allowed values of a <see cref="ThemeSettingKind.Choice"/> setting.</summary>
    public IReadOnlyList<string> Choices { get; init; } = [];

    /// <summary>
    /// For a <see cref="ThemeSettingKind.Color"/> setting, the custom property (such as <c>--brand-accent</c>)
    /// that <c>ThemeHead</c> sets on <c>:root</c>. Only colours can be emitted, because their format is checked
    /// strictly enough to be safe inside a style element.
    /// </summary>
    public string? CssVariable { get; init; }

    /// <summary>The longest <see cref="ThemeSettingKind.Text"/> value accepted.</summary>
    public int MaxLength { get; init; } = 256;
}

public enum ThemeSettingKind
{
    /// <summary>Plain text. Razor encodes it when rendered.</summary>
    Text,

    /// <summary>A hex colour: <c>#rgb</c>, <c>#rrggbb</c> or <c>#rrggbbaa</c>.</summary>
    Color,

    /// <summary><c>true</c> or <c>false</c>.</summary>
    Boolean,

    /// <summary>One of <see cref="ThemeSettingDefinition.Choices"/>.</summary>
    Choice,

    /// <summary>A site-relative path starting with <c>/</c>, or an absolute <c>http</c> or <c>https</c> address.</summary>
    Url,
}
