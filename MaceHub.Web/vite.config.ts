import { defineConfig } from "vite";
import tailwindcss from "@tailwindcss/vite";
import { resolve } from "node:path";

// Vite compiles only TypeScript and Tailwind/daisyUI CSS. htmx is loaded as a plain
// <script> tag in _Layout.cshtml, NOT through this module graph.
//
// Output goes to a fixed path (wwwroot/dist) with fixed filenames (main.js / main.css)
// — no content hashing. For a single-user app, fixed filenames referenced directly
// from _Layout are enough; there is no manifest to read.
export default defineConfig({
  plugins: [tailwindcss()],
  build: {
    outDir: "wwwroot/dist",
    emptyOutDir: true,
    manifest: false,
    rollupOptions: {
      input: {
        main: resolve(__dirname, "src/main.ts"),
      },
      output: {
        entryFileNames: "main.js",
        chunkFileNames: "[name].js",
        assetFileNames: "[name][extname]",
      },
    },
  },
});
