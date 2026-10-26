export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "globalContext",
    alias: "ContentFreeze.GlobalContext",
    name: "Content Freeze Global Context",
    api: () => import("./freeze.context.js"),
  },
];
