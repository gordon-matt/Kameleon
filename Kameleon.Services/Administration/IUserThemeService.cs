namespace Kameleon.Administration;

/// <summary>The signed-in user's own theme choice in the current tenant.</summary>
public interface IUserThemeService
{
    Task<Result<UserThemeChoicesDto>> GetChoicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the user's choice. Null follows the tenant default again. Refused when the tenant does not allow a
    /// choice or the theme is not one it offers; the id is never trusted as given.
    /// </summary>
    Task<Result> SetAsync(string? themeId, CancellationToken cancellationToken = default);
}
