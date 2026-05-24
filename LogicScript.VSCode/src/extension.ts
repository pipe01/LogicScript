import * as path from 'path';
import * as vscode from 'vscode';

import {
	LanguageClient,
	LanguageClientOptions,
	ServerOptions,
	TransportKind
} from 'vscode-languageclient/node';

let client: LanguageClient;

const waitDebugger = false; // TODO: Move to configuration

export async function activate(context: vscode.ExtensionContext) {
	const serverPath = context.asAbsolutePath(
		path.join('bin', process.env.BIN_NAME ?? 'LogicScript.LSP.exe')
	);

	// If the extension is launched in debug mode then the debug server options are used
	// Otherwise the run options are used
	const serverOptions: ServerOptions = {
		run: {
			command: serverPath,
			args: waitDebugger ? ["--wait-debugger"] : [],
			transport: TransportKind.stdio,
		},
		debug: {
			command: "dotnet",
			args: ["run", "--project", context.asAbsolutePath("../LogicScript.DX.LSP/LogicScript.DX.LSP.csproj"), ...(waitDebugger ? ["--", "--wait-debugger"] : [])],
			transport: TransportKind.stdio,
		}
	};

	// Options to control the language client
	const clientOptions: LanguageClientOptions = {
		// Register the server for plain text documents
		documentSelector: [{ scheme: 'file', language: 'logicscript' }],
		traceOutputChannel: vscode.window.createOutputChannel('LogicScript Trace'),
	};

	// Create the language client and start the client.
	client = new LanguageClient(
		'logicscript',
		'LogicScript',
		serverOptions,
		clientOptions
	);

	vscode.commands.registerCommand("logicscript.tests.runFile", () => {
		const currentDocument = vscode.window.activeTextEditor?.document;
		if (!currentDocument) return;

		client.sendRequest("workspace/executeCommand", {
			command: "logicscript.runtestsfile",
			arguments: [currentDocument.uri.toString()]
		})
	});

	const testOutput = vscode.window.createOutputChannel("LogicScript Tests");
	client.onNotification("logicscript/logTestOutput", params => {
		testOutput.appendLine(params);

		if (vscode.workspace.getConfiguration().get<boolean>("logicscript.test.focusOnFail"))
			testOutput.show(true);
	});

	// Start the client. This will also launch the server
	await client.start();
}

export async function deactivate() {
	await client?.stop();
}