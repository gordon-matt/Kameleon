namespace Kameleon.Components;

/// <summary>
/// Where a theme layout shows a region. It shows the page's <see cref="RegionContent"/> when there is some,
/// otherwise the components the host registered for the region, otherwise <see cref="ChildContent"/>.
/// </summary>
public partial class ThemeRegion : IDisposable
{
    private ThemeRegionRegistry? subscribed;

    [CascadingParameter]
    private ThemeRegionRegistry? Registry { get; set; }

    [Parameter, EditorRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>The theme's own content, shown when neither the page nor the host supplies any.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Wraps supplied content, so that markup such as <c>&lt;aside&gt;</c> is only emitted when the region has
    /// something in it. Not applied to <see cref="ChildContent"/>.
    /// </summary>
    [Parameter]
    public RenderFragment<RenderFragment>? Template { get; set; }

    private RenderFragment? Content
    {
        get
        {
            if (Registry is null)
            {
                return null;
            }

            var page = Registry.GetPageContent(Name);
            if (page.Count > 0)
            {
                return builder =>
                {
                    foreach (var fragment in page)
                    {
                        builder.AddContent(0, fragment);
                    }
                };
            }

            var host = Registry.GetHostContent(Name);
            if (host.Count > 0)
            {
                return builder =>
                {
                    foreach (var type in host)
                    {
                        builder.OpenComponent(1, type);
                        builder.CloseComponent();
                    }
                };
            }

            return null;
        }
    }

    public void Dispose()
    {
        if (subscribed is not null)
        {
            subscribed.ContentChanged -= OnContentChanged;
        }
    }

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(subscribed, Registry))
        {
            return;
        }

        Dispose();
        subscribed = Registry;
        if (subscribed is not null)
        {
            subscribed.ContentChanged += OnContentChanged;
        }
    }

    private void OnContentChanged(string region)
    {
        if (string.Equals(region, Name, StringComparison.Ordinal))
        {
            _ = InvokeAsync(StateHasChanged);
        }
    }
}
