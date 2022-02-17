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
                RunFile(file);
            }
        }

        private void RunFile(string scriptPath)
        {
            var (script, errors) = Script.Parse(File.ReadAllText(scriptPath));
            if (errors != null && errors.Count > 0)
            {
                PrintErrors(scriptPath, errors);
                return;
            }
            Debug.Assert(script != null);

            var (bench, benchErrors) = TestBench.Parse(File.ReadAllText(GetBenchPath(scriptPath)), script);
            if (benchErrors != null && benchErrors.Count > 0)
            {
                PrintErrors(scriptPath, benchErrors);
                return;
            }
            Debug.Assert(bench != null);

            using (SimpleConsoleColors.Yellow)
                Console.WriteLine($"Running {bench.CaseCount} tests...");
            Console.WriteLine();

            var results = bench.Run();

            foreach (var result in results)
            {
                if (result.Success)
                {
                    using (SimpleConsoleColors.Green)
                        Console.WriteLine($"✓ {result.Name}");
                }
                else
                {
                    var step = result.FailedStep!.Value;

                    using (SimpleConsoleColors.Red)
                        Console.WriteLine($"✗ {result.Name}, step {step.StepIndex}");
                    Console.WriteLine("  Output values do not match:");

                    foreach (var item in step.MismatchedOutputs)
                    {
                        Console.WriteLine($"    Expected {item.Key} = {step.ExpectedOutputs[item.Key]}, got {item.Value}");
                    }

                    if (FailFast)
                        break;
                }
            }
        }

        private void PrintErrors(string fileName, IReadOnlyList<Parsing.Error> errors)
        {
            Console.WriteLine($"Found {errors.Count} while parsing \"{fileName}\":");

            foreach (var err in errors)
            {
                Console.WriteLine($"  {err.Message} at {err.Span}");
            }
        }

        private static string GetBenchPath(string scriptPath) => Path.ChangeExtension(scriptPath, ".lsbench");
    }
}