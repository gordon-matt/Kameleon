namespace Kameleon.Components;

/// <summary>
/// Supplies a page's content for a theme region, such as a sidebar or a hero banner. Renders nothing where it
/// is placed. Outside a themed layout there is no region to fill, and it does nothing.
/// </summary>
public sealed class RegionContent : ComponentBase, IDisposable
{
    [CascadingParameter]
    private ThemeRegionRegistry? Registry { get; set; }

    /// <summary>A name from <see cref="ThemeRegions"/>, or one a theme documents.</summary>
    [Parameter, EditorRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the child content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    public void Dispose() => Registry?.Remove(this);

    /// <inheritdoc/>
    protected override void OnParametersSet() => Registry?.Set(this, Name, ChildContent);
}
