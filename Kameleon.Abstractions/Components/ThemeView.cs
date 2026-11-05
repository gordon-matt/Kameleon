using Microsoft.AspNetCore.Components.Rendering;

namespace Kameleon.Components;

/// <summary>
/// A named point where the current theme may replace one of the host's components, for example how an article
/// or a search result is presented. The host chooses which views exist and documents the parameters each one
/// receives; a theme can only replace components rendered through this.
/// </summary>
public sealed class ThemeView : ComponentBase
{
    [CascadingParameter]
    private ThemeContext? Theme { get; set; }

    /// <summary>The view name. Prefix it with your application or library name to keep it unique.</summary>
    [Parameter, EditorRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>The host's own component, used when the theme does not replace this view.</summary>
    [Parameter]
    public Type? Fallback { get; set; }

    /// <summary>Passed to whichever component is rendered. Both must accept every key.</summary>
    [Parameter]
    public IDictionary<string, object?>? Parameters { get; set; }

    /// <summary>Rendered when neither the theme nor <see cref="Fallback"/> supplies a component.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var type = Theme?.Theme?.Views.GetValueOrDefault(Name) ?? Fallback;
        if (type is null)
        {
            builder.AddContent(0, ChildContent);
            return;
        }

        builder.OpenComponent<DynamicComponent>(1);
        builder.AddAttribute(2, nameof(DynamicComponent.Type), type);
        builder.AddAttribute(3, nameof(DynamicComponent.Parameters), Parameters);
        builder.CloseComponent();
    }
}
