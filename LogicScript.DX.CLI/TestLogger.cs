using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogicScript.Parsing;
using LogicScript.Testing;
using LogicScript.Testing.Results;
using SimpleConsoleColor;

namespace LogicScript.DX.CLI
{
    public interface ITestLogger
    {
        void LogParseErrors(string fileName, IReadOnlyList<Parsing.Error> errors);
        void LogStartBench(TestBench bench);
        void LogResult(CaseResult result);
        void LogEndBench(TestBench bench, int successful, int failed);
    }

    public class PrettyTestLogger : ITestLogger
    {
        public void LogStartBench(TestBench bench)
        {
            using (SimpleConsoleColors.Yellow)
                Console.WriteLine($"Running {bench.CaseCount} tests...");
            Console.WriteLine();
        }

        public void LogResult(CaseResult result)
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
            }
        }

        public void LogEndBench(TestBench bench, int successful, int failed)
        {
            Console.WriteLine();
            using (failed == 0 ? SimpleConsoleColors.Green : SimpleConsoleColors.Red)
                Console.WriteLine($"{successful} cases passed, {failed} failed");
        }

        public void LogParseErrors(string fileName, IReadOnlyList<Error> errors)
        {
            using (SimpleConsoleColors.Red)
                Console.WriteLine($"Found {errors.Count} errors when parsing \"{fileName}\":");

            foreach (var err in errors)
            {
                Console.WriteLine($"• {err.Message} at {err.Span}");
            }
        }
    }
}