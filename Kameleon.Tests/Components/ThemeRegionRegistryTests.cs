namespace Kameleon.Tests.Components;

public sealed class ThemeRegionRegistryTests
{
    private static readonly RenderFragment Fragment = _ => { };

    [Fact]
    public void Set_SecondContentForSameRegion_RaisesPresenceOnlyOnce()
    {
        var registry = new ThemeRegionRegistry();
        var presence = new List<string>();
        var content = new List<string>();
        registry.PresenceChanged += presence.Add;
        registry.ContentChanged += content.Add;

        registry.Set(new object(), ThemeRegions.Sidebar, Fragment);
        registry.Set(new object(), ThemeRegions.Sidebar, Fragment);

        Assert.Equal([ThemeRegions.Sidebar], presence);
        Assert.Equal(2, content.Count);
    }

    [Fact]
    public void Remove_LastContent_RaisesPresence()
    {
        var registry = new ThemeRegionRegistry();
        var owner = new object();
        registry.Set(owner, ThemeRegions.Hero, Fragment);
        var presence = new List<string>();
        registry.PresenceChanged += presence.Add;

        registry.Remove(owner);

        Assert.Equal([ThemeRegions.Hero], presence);
        Assert.False(registry.HasContent(ThemeRegions.Hero));
    }

    [Fact]
    public void Set_SameOwnerSameRegion_ReplacesContentWithoutPresenceChange()
    {
        var registry = new ThemeRegionRegistry();
        var owner = new object();
        registry.Set(owner, ThemeRegions.Footer, Fragment);
        var presence = new List<string>();
        registry.PresenceChanged += presence.Add;

        registry.Set(owner, ThemeRegions.Footer, _ => { });

        Assert.Empty(presence);
        Assert.Single(registry.GetPageContent(ThemeRegions.Footer));
    }

    [Fact]
    public void HasContent_HostContentOnly_IsTrue()
    {
        var registry = new ThemeRegionRegistry(new Dictionary<string, IReadOnlyList<Type>> { [ThemeRegions.PrimaryNavigation] = [typeof(TestView)] });

        Assert.True(registry.HasContent(ThemeRegions.PrimaryNavigation));
        Assert.Empty(registry.GetPageContent(ThemeRegions.PrimaryNavigation));
    }
}
