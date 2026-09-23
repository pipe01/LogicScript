using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogicScript.Data;

namespace LogicScript.Testing.Results
{
    public abstract class CaseResult
    {
        public TestCase TestCase { get; }
        public IReadOnlyCollection<string> PrintedLines { get; }

        public abstract bool Success { get; }

        internal CaseResult(TestCase testCase, IReadOnlyCollection<string> printedLines)
        {
            this.TestCase = testCase;
            this.PrintedLines = printedLines;
        }
    }

    public sealed class SuccessStepResult : CaseResult
    {
        public override bool Success => true;

        internal SuccessStepResult(TestCase testCase, IReadOnlyCollection<string> printedLines) : base(testCase, printedLines)
        {
        }
    }

    public sealed class FailedStepCaseResult : CaseResult
    {
        public override bool Success => false;

        public int StepIndex { get; }
        public CaseStep Step { get; }
        public string StepSource { get; }
        public IDictionary<string, BitsValue[]> ExpectedOutputs { get; }
        public IDictionary<string, BitsValue[]> MismatchedOutputs { get; }

        internal FailedStepCaseResult(TestCase testCase, IReadOnlyCollection<string> printedLines, int stepIndex, CaseStep step, string stepSource, IDictionary<string, BitsValue[]> expectedOutputs, IDictionary<string, BitsValue[]> mismatchedOutputs) : base(testCase, printedLines)
        {
            this.StepIndex = stepIndex;
            this.Step = step;
            this.StepSource = stepSource;
            this.ExpectedOutputs = expectedOutputs;
            this.MismatchedOutputs = mismatchedOutputs;
        }

        public string GetFailureString()
        {
            if (Success) throw new InvalidOperationException("Test was successful");

            var msg = new StringBuilder();
            msg.AppendLine($"Failed on step {StepIndex} at {Step.Span.Start.FileName}:{Step.Span.Start}");

            var mismatches = MismatchedOutputs.Keys.ToArray();

            msg.AppendLine($"            Step: {StepSource.Trim()}");
            msg.AppendLine($" Expected output: {FormatIO(ExpectedOutputs, mismatches)}");
            msg.AppendLine($"      Got output: {FormatIO(MismatchedOutputs, mismatches)}");

            return msg.ToString();

            static string FormatIO(IDictionary<string, BitsValue[]> values, IEnumerable<string> keys)
            {
                return string.Join(' ', keys.Select(k => $"{k}({string.Join(", ", values[k])})").ToArray());
            }
        }
    }

    public sealed class LimitReachedCaseResult : CaseResult
    {
        public override bool Success => false;

        public int StepsRan { get; }

        internal LimitReachedCaseResult(TestCase testCase, IReadOnlyCollection<string> printedLines, int stepsRan) : base(testCase, printedLines)
        {
            this.StepsRan = stepsRan;
        }
    }
}