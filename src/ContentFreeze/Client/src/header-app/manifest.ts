export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "headerApp",
    alias: "ContentFreeze.HeaderApp",
    name: "Content Freeze Header App",
    element: () => import("./freeze-header-app.element.js"),
    // Left of search (900), so it is the first thing in the top bar.
    weight: 950,
  },
];
