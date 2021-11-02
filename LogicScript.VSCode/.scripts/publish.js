const { exec } = require("child_process");
const { readdirSync } = require("fs");

const fileArgs = readdirSync("vsix").map(o => `-i "vsix/${o}"`).join(" ");

const proc = exec(`vsce publish ${fileArgs}`);
proc.stdout.pipe(process.stdout);
proc.stderr.pipe(process.stderr);

proc.addListener("exit", code => process.exit(code));