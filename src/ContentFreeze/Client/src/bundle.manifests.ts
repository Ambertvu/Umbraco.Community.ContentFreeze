import { manifests as conditions } from "./conditions/manifest.js";
import { manifests as context } from "./context/manifest.js";
import { manifests as dashboards } from "./dashboard/manifest.js";
import { manifests as entrypoints } from "./entrypoints/manifest.js";
import { manifests as headerApps } from "./header-app/manifest.js";
import { manifests as permissions } from "./permissions/manifest.js";
import { manifests as workspace } from "./workspace/manifest.js";

// Job of the bundle is to collate all the manifests from different parts of the extension and load other manifests
// We load this bundle from umbraco-package.json
export const manifests: Array<UmbExtensionManifest> = [
  ...entrypoints,
  ...context,
  ...conditions,
  ...headerApps,
  ...workspace,
  ...permissions,
  ...dashboards,
];
