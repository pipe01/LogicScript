const esbuild = require("esbuild");
const argv = require("minimist")(process.argv.slice(2));

if (!argv.bin) {
    console.error("Binary name is required");
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
        "process.env.BIN_NAME": JSON.stringify(argv.bin)
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
