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

	const testsController = vscode.tests.createTestController("logicscriptTests", "LogicScript Tests");
	context.subscriptions.push(testsController);

	const mapFileTests = new Map<string, vscode.TestItem>();
	client.onNotification("logicscript/foundTests", (args: { uri: string, tests: { id: string, name: string, range: vscode.Range }[] }) => {
		const uri = vscode.Uri.parse(args.uri, true);

		var fileTest = mapFileTests.get(args.uri);
		if (!fileTest) {
			fileTest = testsController.createTestItem(args.uri, path.basename(uri.path), uri);
			testsController.items.add(fileTest);

			mapFileTests.set(args.uri, fileTest);
		}
		fileTest.children.forEach(i => fileTest!.children.delete(i.id));

		for (const test of args.tests) {
			const testItem = testsController.createTestItem(test.id, test.name, vscode.Uri.parse(args.uri, true));
			testItem.range = test.range;
			fileTest.children.add(testItem);
		}
	});

	testsController.createRunProfile(
		'Run',
		vscode.TestRunProfileKind.Run,
		runTests(false, testsController, mapFileTests),
		true
	);
	testsController.createRunProfile(
		'Debug',
		vscode.TestRunProfileKind.Debug,
		runTests(true, testsController, mapFileTests),
	);

	context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory("logicscript-test", {
		async createDebugAdapterDescriptor(session, _) {
			const dapEndpoint: { host: string, port: number } = await client.sendRequest("workspace/executeCommand", {
				command: "logicscript/startTestDebug"
			});

			return new vscode.DebugAdapterServer(dapEndpoint.port);
		},
	}));

	const runTestsButton = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left);
	runTestsButton.text = "$(run-all) Run all tests";
	runTestsButton.command = "logicscript.tests.runFile";
	context.subscriptions.push(runTestsButton);

	const debugTestsButton = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left);
	debugTestsButton.text = "$(bug) Debug all tests";
	debugTestsButton.command = {
		command: "logicscript.tests.debugFile",
		title: "debug",
		arguments: [vscode.window.activeTextEditor!.document.uri.toString(), null]
	};
	context.subscriptions.push(debugTestsButton);

	if (vscode.window.activeTextEditor?.document.languageId === "logicscript") {
		runTestsButton.show();
		debugTestsButton.show();
	}

	context.subscriptions.push(vscode.window.onDidChangeActiveTextEditor((editor) => {
		if (editor?.document.languageId === "logicscript") {
			runTestsButton.show();
			debugTestsButton.show();
		}
		else {
			runTestsButton.hide();
			debugTestsButton.show();
		}
	}));

	// Start the client. This will also launch the server
	await client.start();
}

export async function deactivate() {
	await client?.stop();
}

function runTests(debug: boolean, testsController: vscode.TestController, mapFileTests: Map<string, vscode.TestItem>) {
	return async (request: vscode.TestRunRequest, token: vscode.CancellationToken) => {
		const run = testsController.createTestRun(request);

		const tests: vscode.TestItem[] = [];

		if (request.include) {
			for (const include of request.include) {
				if (mapFileTests.has(include.id)) {
					const file = mapFileTests.get(include.id)!;
					file.children.forEach(t => tests.push(t));
				}
				else {
					tests.push(include);
				}
			}
		}
		else
		{
			[...mapFileTests.values()].forEach(m => m.children.forEach(t => tests.push(t)));
		}

		if (debug) {
			await vscode.debug.startDebugging(undefined, {
				name: "Test debug",
				request: "attach",
				type: "logicscript-test",
			})
		}

		for (const item of tests) {
			run.started(item);

			try {
				const resp: { success: boolean; scriptOutput: string[]; result: string; } = await client.sendRequest("workspace/executeCommand", {
					command: "logicscript/runTest",
					arguments: [
						item.uri?.toString(),
						item.id,
						debug,
					]
				}, token);

				run.appendOutput(resp.scriptOutput.join("\n"), undefined, item);

				run.appendOutput(resp.result);

				if (resp.success)
					run.passed(item);

				else
					run.failed(item, new vscode.TestMessage("Test failed"));
			} catch (err) {
				run.failed(item, new vscode.TestMessage(String(err)));
			}
		}

		if (debug)
			await vscode.debug.stopDebugging();

		run.end();
	};
}
