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

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    public void Dispose() => Registry?.Remove(this);

    protected override void OnParametersSet() => Registry?.Set(this, Name, ChildContent);
}
