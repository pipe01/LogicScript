using LogicScript.Parsing;
using LogicScript.Testing;
using LogicScript.Testing.Results;
using SimpleConsoleColor;

namespace LogicScript.DX.CLI
{
    public interface ITestLogger
    {
        void LogParseErrors(string fileName, IReadOnlyList<Error> errors);
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
            switch (result)
            {
                case SuccessStepResult:
                    using (SimpleConsoleColors.Green)
                        Console.WriteLine($"✓ {result.TestCase.Name}");
                    break;

                case FailedStepCaseResult failedStep:
                    using (SimpleConsoleColors.Red)
                        Console.WriteLine($"✗ {failedStep.TestCase.Name}, step {failedStep.StepIndex}");
                    Console.WriteLine("  Output values do not match:");

                    foreach (var item in failedStep.MismatchedOutputs)
                    {
                        Console.WriteLine($"    Expected {item.Key} = {failedStep.ExpectedOutputs[item.Key]}, got {item.Value}");
                    }
                    break;

                case LimitReachedCaseResult:
                    Console.WriteLine("✗ Statement limit reached");
                    break;
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