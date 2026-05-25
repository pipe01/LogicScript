const esbuild = require("esbuild");
const argv = require("minimist")(process.argv.slice(2));

if (!argv.lsp) {
    console.error("LSP binary name is required");
    process.exit(1);
}

/**
 * @type import("esbuild").BuildOptions
 */
const options = {
    entryPoints: ["./src/extension.ts"],
    bundle: true,
    outfile: "out/main.js",
    external: ["vscode"],
    format: "cjs",
    platform: "node",
    define: {
        "process.env.LSP_NAME": JSON.stringify(argv.lsp)
    }
};

if (argv.watch) {
    options.watch = true;
    options.sourcemap = true;
}
if (argv.minify) {
    options.minify = true;
}

esbuild.build(options)
    .then(() => console.log("Built successfully"))
    .catch(o => console.error("Failed to build", o));
