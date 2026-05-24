using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogicScript.Data;
using LogicScript.Parsing;

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
        public SourceSpan StepSpan { get; }
        public IDictionary<string, BitsValue> Inputs { get; }
        public IDictionary<string, BitsValue> ExpectedOutputs { get; }
        public IDictionary<string, BitsValue> MismatchedOutputs { get; }

        internal FailedStepCaseResult(TestCase testCase, IReadOnlyCollection<string> printedLines, int stepIndex, SourceSpan stepSpan, IDictionary<string, BitsValue> inputs, IDictionary<string, BitsValue> expectedOutputs, IDictionary<string, BitsValue> mismatchedOutputs) : base(testCase, printedLines)
        {
            this.StepIndex = stepIndex;
            this.StepSpan = stepSpan;
            this.Inputs = inputs;
            this.ExpectedOutputs = expectedOutputs;
            this.MismatchedOutputs = mismatchedOutputs;
        }

        public string GetFailureString()
        {
            if (Success) throw new InvalidOperationException("Test was successful");

            var msg = new StringBuilder();
            msg.AppendLine($"Failed on step {StepIndex} at {StepSpan.Start.FileName}:{StepSpan.Start}");

            msg.AppendLine($"           Input: {FormatIO(Inputs)}");
            msg.AppendLine($" Expected output: {FormatIO(ExpectedOutputs)}");
            msg.AppendLine($"Disparate output: {FormatIO(MismatchedOutputs)}");

            return msg.ToString();

            static string FormatIO(IDictionary<string, BitsValue> values)
            {
                return string.Join(' ', values.OrderBy(e => e.Key).Select(e => $"{e.Key}({e.Value.Number})").ToArray());
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