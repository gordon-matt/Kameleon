using Heroicsoft.NetForge.Components.Layout;

namespace Kameleon.Components.Admin;

/// <summary>
/// The layout of the theme screens: the host's <see cref="KameleonOptions.AdminLayout"/> when it set one,
/// otherwise the NetForge shell. Either must mount the NetForge dialog host, which the shell does.
/// </summary>
public partial class KameleonAdminLayout
{
    [Inject]
    private IOptions<KameleonOptions> Options { get; set; } = default!;

    private Type Layout => Options.Value.AdminLayout ?? typeof(ShellLayout);
}
