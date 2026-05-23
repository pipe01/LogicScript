using LogicScript.DX.DAP;
using LogicScript.Interpreting.Debugging;
using LogicScript.Testing;

namespace LogicScript.DX.CLI.Commands
{
    public class TestCommand
    {
        public IReadOnlyList<string>? Files { get; set; }
        public bool FailFast { get; set; }
        public bool Debug { get; set; }

        public async Task RunAsync()
        {
            IDebugger? debugger = null;
            if (Debug)
            {
                Console.WriteLine("Waiting for debugger connection...");
                debugger = await LogicScriptDebugger.LaunchAndWaitForAttachedAsync();
            }

            if (Files == null)
            {
                Files = Directory.EnumerateFiles(".", "*.lsx").Where(o => File.Exists(GetBenchPath(o))).ToList();
            }
            else
            {
                foreach (var file in Files)
                {
                    if (!File.Exists(GetBenchPath(file)))
                    {
                        throw new Exception($"No test bench found for file \"{file}\"");
                    }
                }
            }

            foreach (var file in Files)
            {
                await RunFileAsync(Path.GetFullPath(file), new PrettyTestLogger(), debugger);
            }
        }

        private async Task RunFileAsync(string scriptPath, ITestLogger logger, IDebugger? debugger)
        {
            var (script, errors) = Script.Parse(File.ReadAllText(scriptPath), scriptPath);
            if (errors != null && errors.Count > 0)
            {
                logger.LogParseErrors(scriptPath, errors);
                return;
            }
            if (script == null)
                throw new Exception("Failed to parse script");

            var (bench, benchErrors) = TestBench.Parse(File.ReadAllText(GetBenchPath(scriptPath)), script);
            if (benchErrors != null && benchErrors.Count > 0)
            {
                logger.LogParseErrors(scriptPath, benchErrors);
                return;
            }
            if (bench == null)
                throw new Exception("Failed to parse bench");

            logger.LogStartBench(bench);

            var results = bench.Run(debugger);
            int successful = 0, failed = 0;

            await foreach (var result in results)
            {
                logger.LogResult(result);

                if (result.Success)
                {
                    successful++;
                }
                else
                {
                    failed++;

                    if (FailFast)
                        break;
                }
            }

            logger.LogEndBench(bench, successful, failed);
        }

        private static string GetBenchPath(string scriptPath) => Path.ChangeExtension(scriptPath, ".lsbench");
    }
}