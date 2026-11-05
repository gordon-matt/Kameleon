namespace Kameleon;

/// <summary>Which tenants may use an installed theme. A tenant still has to enable a theme it may use.</summary>
public enum ThemeAvailability
{
    AllTenants = 0,

    /// <summary>Only tenants an administrator has granted the theme to.</summary>
    SelectedTenants = 1,
}
