import * as path from 'path';
import * as vscode from 'vscode';

import {
	LanguageClient,
	LanguageClientOptions,
	ServerOptions,
	TransportKind
} from 'vscode-languageclient/node';

let client: LanguageClient;

const waitDebugger = false;

export async function activate(context: vscode.ExtensionContext) {
	const lspPath = context.asAbsolutePath(
		path.join('bin', process.env.LSP_NAME ?? 'LogicScript.LSP.exe')
	);
	const lspArgs = waitDebugger ? ["--wait-debugger"] : [];

	const cliPath = context.asAbsolutePath(
		path.join('bin', process.env.CLI_NAME ?? 'LogicScript.CLI.exe')
	);

	// If the extension is launched in debug mode then the debug server options are used
	// Otherwise the run options are used
	const serverOptions: ServerOptions = {
		run: {
			command: lspPath,
			args: lspArgs,
			transport: TransportKind.stdio,
		},
		debug: {
			command: "dotnet",
			args: ["run", "--project", context.asAbsolutePath("../LogicScript.DX.LSP/LogicScript.DX.LSP.csproj"), ...lspArgs],
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

	const config = vscode.workspace.getConfiguration("logicscript");

	vscode.commands.registerCommand("logicscript.tests.runFile", () => {
		const currentDocument = vscode.window.activeTextEditor?.document;
		if (!currentDocument) return;

		client.sendRequest("workspace/executeCommand", {
			command: "logicscript.runtestsfile",
			arguments: [
				currentDocument.uri.toString(),
				config.get("test.statementLimit")
			]
		})
	});

	vscode.commands.registerCommand("logicscript.tests.debugFile", (script, caseIndex) => {
		vscode.debug.startDebugging(undefined, {
			name: "Test debug",
			request: "attach",
			type: "logicscript-test",
			script,
			caseIndices: [caseIndex],
		})
	})

	const testOutput = vscode.window.createOutputChannel("LogicScript Tests");
	client.onNotification("logicscript/clearTestOutput", () => testOutput.clear());
	client.onNotification("logicscript/logTestOutput", params => {
		testOutput.appendLine(params);

		if (config.get<boolean>("test.focusOnFail"))
			testOutput.show(true);
	});
	context.subscriptions.push(testOutput);

	context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory("logicscript-test", {
		async createDebugAdapterDescriptor(session, _) {
			const scriptPath = (session.configuration.script as string) ?? vscode.window.activeTextEditor?.document.uri.toString();
			const caseIndices = session.configuration.caseIndices as number[] | undefined;

			const dapEndpoint: { host: string, port: number } = await client.sendRequest("workspace/executeCommand", {
				command: "logicscript/startTestDebug",
				arguments: [
					scriptPath,
					...(caseIndices ? [caseIndices] : [])
				]
			});

			return new vscode.DebugAdapterServer(dapEndpoint.port);
		},
	}));

	const runTestsButton = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left);
	runTestsButton.text = "$(run-all) Run all tests";
	runTestsButton.command = "logicscript.tests.runFile";
	context.subscriptions.push(runTestsButton);

	if (vscode.window.activeTextEditor?.document.languageId === "logicscript")
		runTestsButton.show();

	context.subscriptions.push(vscode.window.onDidChangeActiveTextEditor((editor) => {
		if (editor?.document.languageId === "logicscript")
			runTestsButton.show();
		else
			runTestsButton.hide();
	}));

	// Start the client. This will also launch the server
	await client.start();
}

export async function deactivate() {
	await client?.stop();
}
