import { defineConfig } from "vite";

export default defineConfig({
  build: {
    lib: {
      entry: {
        "content-freeze": "src/bundle.manifests.ts", // Bundle registers one or more manifests
        // Loaded on the login screen through a separate public manifest, see LoginNoticeManifestReader.
        "login-notice": "src/login/login-notice.ts",
      },
      formats: ["es"],
      fileName: (_format, entryName) => `${entryName}.js`,
    },
    outDir: "../wwwroot/App_Plugins/ContentFreeze", // your web component will be saved in this location
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco/],
    },
  },
});
