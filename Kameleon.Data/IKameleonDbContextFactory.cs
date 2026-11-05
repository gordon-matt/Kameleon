namespace Kameleon.Data;

/// <summary>
/// Kameleon's own context factory. It extends rather than replaces Extenso's <see cref="IDbContextFactory"/>:
/// a host built on Extenso registers that interface for its own context, and Kameleon must not take it over.
/// Its repositories are built against this type instead.
/// </summary>
public interface IKameleonDbContextFactory : IDbContextFactory
{
    /// <summary>One of <see cref="Constants.DatabaseProviders"/>.</summary>
    string ProviderName { get; }
}
