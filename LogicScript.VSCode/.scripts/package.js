const cp = require("child_process");
const { mkdirSync } = require("fs");
const path = require("path");

const platforms = {
    "Windows x64":  { vsce: "win32-x64", dotnet: "win-x64", ext: ".exe" },
    "Linux x64":    { vsce: "linux-x64", dotnet: "linux-x64", ext: "" },
}

const outFolder = "vsix";

try {
    mkdirSync(outFolder);
} catch (e) {}

for (const name in platforms) {
    console.log(`Building LSP server for ${name}`);

    const plat = platforms[name];

    cp.execSync(`dotnet publish -c Release -r "${plat.dotnet}" -o bin /p:PublishSingleFile=true ../LogicScript.DX.LSP/LogicScript.DX.LSP.csproj`);

    console.log("Bundling code");
    cp.execSync(`node .scripts/build.js --minify --bin "LogicScript.DX.LSP${plat.ext}"`);

    const outPath = path.join(outFolder, `logicscript-lang-${plat.vsce}.vsix`)

    console.log(`Packaging VSIX for ${name}`);
    cp.execSync(`npx vsce package --target "${plat.vsce}" --out "${outPath}"`);

    console.log();

    require("rimraf").sync("bin");
}
