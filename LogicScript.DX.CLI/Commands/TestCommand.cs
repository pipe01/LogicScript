using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LogicScript.Testing;
using SimpleConsoleColor;

namespace LogicScript.DX.CLI.Commands
{
    public class TestCommand
    {
        public IReadOnlyList<string>? Files { get; set; }
        public bool FailFast { get; set; }

        public void Run()
        {
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
                RunFile(file, new PrettyTestLogger());
            }
        }

        private void RunFile(string scriptPath, ITestLogger logger)
        {
            var (script, errors) = Script.Parse(File.ReadAllText(scriptPath));
            if (errors != null && errors.Count > 0)
            {
                logger.LogParseErrors(scriptPath, errors);
                return;
            }
            Debug.Assert(script != null);

            var (bench, benchErrors) = TestBench.Parse(File.ReadAllText(GetBenchPath(scriptPath)), script);
            if (benchErrors != null && benchErrors.Count > 0)
            {
                logger.LogParseErrors(scriptPath, benchErrors);
                return;
            }
            Debug.Assert(bench != null);

            logger.LogStartBench(bench);

            var results = bench.Run();
            int successful = 0, failed = 0;

            foreach (var result in results)
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