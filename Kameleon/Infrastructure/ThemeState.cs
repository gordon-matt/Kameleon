namespace Kameleon.Infrastructure;

/// <summary>
/// The resolved theme of one request or circuit, shared by the layout and <see cref="ThemeHead"/> so the theme
/// is resolved once. When an administrator or the user changes something that affects this circuit, the
/// theme is resolved again and <see cref="Changed"/> fires.
/// </summary>
internal sealed class ThemeState : IDisposable
{
    private readonly IThemeResolver resolver;
    private readonly IThemeChangeNotifier notifier;
    private readonly SemaphoreSlim gate = new(1, 1);
    private ThemeContext? current;

    public ThemeState(IThemeResolver resolver, IThemeChangeNotifier notifier)
    {
        this.resolver = resolver;
        this.notifier = notifier;
        notifier.Changed += OnChanged;
    }

    /// <summary>Raised after the theme was resolved again. It can fire on any thread.</summary>
    public event Action<ThemeContext>? Changed;

    public async Task<ThemeContext> GetAsync(CancellationToken cancellationToken = default)
    {
        if (current is { } resolved)
        {
            return resolved;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            return current ??= await resolver.ResolveAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose()
    {
        notifier.Changed -= OnChanged;
        gate.Dispose();
    }

    private void OnChanged(object? sender, ThemeChangedEventArgs args)
    {
        if (current is not { } shown || !args.Affects(shown.TenantId, shown.UserId))
        {
            return;
        }

        current = null;
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            var resolved = await GetAsync();
            Changed?.Invoke(resolved);
        }
        catch (ObjectDisposedException)
        {
            // The circuit closed while the theme was being resolved again.
        }
    }
}
