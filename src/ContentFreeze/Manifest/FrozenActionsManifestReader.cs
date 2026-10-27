using ContentFreeze.Enforcement;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace ContentFreeze.Manifest;

/// <summary>
/// Hands the aliases from <see cref="IFrozenActionsProvider"/>s to the backoffice, which hides those actions while
/// editing is frozen. Add-ons can do this from C#, without a client build of their own.
/// </summary>
internal class FrozenActionsManifestReader : IPackageManifestReader
{
    /// <summary>Read by the backoffice entry point, see Client/src/entrypoints/entrypoint.ts.</summary>
    internal const string ExtensionType = "contentFreezeFrozenActions";

    private readonly IEnumerable<IFrozenActionsProvider> _providers;

    public FrozenActionsManifestReader(IEnumerable<IFrozenActionsProvider> providers) => _providers = providers;

    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        object[] extensions = _providers
            .Select(provider => (Name: provider.GetType().Name, Aliases: provider.GetFrozenActionAliases().Distinct().ToArray()))
            .Where(provider => provider.Aliases.Length > 0)
            .Select(provider => (object)new
            {
                type = ExtensionType,
                alias = $"ContentFreeze.FrozenActions.{provider.Name}",
                name = $"Content Freeze frozen actions ({provider.Name})",
                meta = new { aliases = provider.Aliases },
            })
            .ToArray();

        return Task.FromResult<IEnumerable<PackageManifest>>(extensions.Length == 0
            ? []
            : [new PackageManifest { Name = "ContentFreeze.FrozenActions", AllowTelemetry = false, Extensions = extensions }]);
    }
}
