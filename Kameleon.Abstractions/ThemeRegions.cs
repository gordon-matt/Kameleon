namespace Kameleon;

/// <summary>
/// Conventional region names, so that pages and themes written by different people line up. Regions are a
/// convention, not a contract: a theme may leave any of them out, reorder them or add regions of its own.
/// </summary>
public static class ThemeRegions
{
    public const string Header = "Header";
    public const string PrimaryNavigation = "PrimaryNavigation";
    public const string Breadcrumbs = "Breadcrumbs";
    public const string Hero = "Hero";
    public const string Sidebar = "Sidebar";
    public const string Aside = "Aside";
    public const string Footer = "Footer";
    public const string Overlay = "Overlay";
}
