namespace Kameleon.Components;

/// <summary>
/// The current theme's stylesheets, colour settings and scripts. Place it in the document's <c>&lt;head&gt;</c>
/// itself rather than in <c>HeadContent</c>, because a page's own <c>HeadContent</c> replaces any other.
/// This markup is the first paint. While a circuit is open, a change of theme replaces these assets in
/// place, so the next page keeps the layout and the styles together.
/// </summary>
public partial class ThemeHead : IDisposable
{
    private ThemeContext? context;
    private string? variables;

    [Inject]
    private ThemeState State { get; set; } = default!;

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    public void Dispose() => State.Changed -= OnThemeChanged;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        State.Changed += OnThemeChanged;
        Apply(await State.GetAsync());
    }

    private void Apply(ThemeContext resolved)
    {
        context = resolved;
        variables = ThemeStyles.BuildRootVariables(resolved);
    }

    private void OnThemeChanged(ThemeContext resolved) => _ = InvokeAsync(() =>
    {
        Apply(resolved);
        StateHasChanged();
    });
}
