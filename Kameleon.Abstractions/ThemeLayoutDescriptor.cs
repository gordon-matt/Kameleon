namespace Kameleon;

/// <summary>One page layout a theme offers.</summary>
/// <param name="Id">Lowercase letters, digits and single hyphens. Stored in tenant settings, so keep it stable.</param>
/// <param name="Name">Shown to administrators.</param>
/// <param name="ComponentType">
/// A <see cref="LayoutComponentBase"/>. It receives the page as <c>Body</c> and the current
/// <see cref="ThemeContext"/> as a cascading value; <see cref="ThemeLayoutBase"/> wires both up.
/// </param>
public sealed record ThemeLayoutDescriptor(string Id, string Name, Type ComponentType)
{
    public string? Description { get; init; }
}
