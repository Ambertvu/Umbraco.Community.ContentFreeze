export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Content Freeze Entrypoint",
    alias: "ContentFreeze.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
