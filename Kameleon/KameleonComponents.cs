using System.Reflection;

namespace Kameleon;

/// <summary>
/// Provides kameleon components.
/// </summary>
public static class KameleonComponents
{
    /// <summary>
    /// The assembly holding the ready-made theme screens at <see cref="KameleonOptions.DefaultAdminPath"/>. Add it
    /// to the router's <c>AdditionalAssemblies</c> and to <c>MapRazorComponents().AddAdditionalAssemblies</c>.
    /// A host that puts the screens elsewhere leaves it out and renders the screen components from its own pages.
    /// </summary>
    public static Assembly Assembly => typeof(KameleonComponents).Assembly;
}
