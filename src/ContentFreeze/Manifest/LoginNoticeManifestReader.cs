using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace ContentFreeze.Manifest;

/// <summary>
/// A second, public package manifest with only the login screen notice. The login screen loads public
/// manifests only; making the main manifest public would load the whole backoffice UI there.
/// </summary>
internal class LoginNoticeManifestReader : IPackageManifestReader
{
    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
        => Task.FromResult<IEnumerable<PackageManifest>>(
        [
            new PackageManifest
            {
                Name = "ContentFreeze.LoginNotice",
                AllowPublicAccess = true,
                AllowTelemetry = false,
                Extensions =
                [
                    new
                    {
                        type = "appEntryPoint",
                        alias = "ContentFreeze.LoginNotice",
                        name = "Content Freeze Login Notice",
                        js = "/App_Plugins/ContentFreeze/login-notice.js",
                    },
                ],
            },
        ]);
}
