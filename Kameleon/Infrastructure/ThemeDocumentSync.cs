using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Kameleon.Infrastructure;

/// <summary>
/// Keeps the document head on the theme the circuit is showing. <see cref="ThemeHead"/> renders the
/// assets for the first paint and is not part of the circuit, so a later change would otherwise leave
/// the previous theme's styles in place until a full reload.
/// </summary>
internal sealed class ThemeDocumentSync(
    ThemeState state,
    IJSRuntime js,
    NavigationManager navigation,
    ILogger<ThemeDocumentSync> logger) : CircuitHandler, IDisposable
{
    public void Dispose() => state.Changed -= OnThemeChanged;

    /// <inheritdoc/>
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        state.Changed += OnThemeChanged;
        return Task.CompletedTask;
    }

    private void OnThemeChanged(ThemeContext resolved)
    {
        var theme = resolved.Theme;
        _ = ApplyAsync(
            theme?.StylesheetUrls.ToArray() ?? [],
            ThemeStyles.BuildRootVariables(resolved),
            theme?.ScriptUrls.ToArray() ?? []);
    }

    private async Task ApplyAsync(string[] stylesheets, string? variables, string[] scripts)
    {
        try
        {
            await js.InvokeVoidAsync("kameleon.applyDocument", stylesheets, variables, scripts);
        }
        catch (Exception exception) when (exception is JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
        {
            // The circuit closed while the new assets were being applied.
        }
        catch (Exception exception)
        {
            // A full load still paints the theme the server resolved.
            logger.LogError(exception, "The theme assets could not be updated; reloading the page");
            TryReload();
        }
    }

    private void TryReload()
    {
        try
        {
            navigation.Refresh(forceReload: true);
        }
        catch (Exception exception) when (exception is ObjectDisposedException or InvalidOperationException)
        {
            // The circuit is already closing.
        }
    }
}
