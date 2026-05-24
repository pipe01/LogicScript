using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogicScript.Data;

namespace LogicScript.Testing.Results
{
    public readonly record struct FailedStep(
        int StepIndex,
        IDictionary<string, BitsValue> Inputs,
        IDictionary<string, BitsValue> ExpectedOutputs,
        IDictionary<string, BitsValue> MismatchedOutputs
    );

    public class CaseResult
    {
        public int Index { get; }
        public string? Name { get; }
        public int StepCount { get; }
        public IReadOnlyCollection<string> PrintedLines { get; }

        public bool Success { get; }
        public FailedStep? FailedStep { get; }

        internal CaseResult(int index, string? name, int stepCount, IReadOnlyCollection<string> printedLines)
        {
            this.Index = index;
            this.Name = name;
            this.StepCount = stepCount;
            this.Success = true;
            this.PrintedLines = printedLines;
        }

        internal CaseResult(int index, string? name, int stepCount, IReadOnlyCollection<string> printedLines, FailedStep? failedStep) : this(index, name, stepCount, printedLines)
        {
            this.FailedStep = failedStep;
            this.Success = false;
        }

        public string GetFailureString()
        {
            if (Success) throw new InvalidOperationException("Test was successful");

            var msg = new StringBuilder();
            msg.AppendLine($"Failed on step {FailedStep!.Value.StepIndex}:");

            msg.AppendLine($"           Input: {FormatIO(FailedStep.Value.Inputs)}");
            msg.AppendLine($" Expected output: {FormatIO(FailedStep.Value.ExpectedOutputs)}");
            msg.AppendLine($"Disparate output: {FormatIO(FailedStep.Value.MismatchedOutputs)}");

            return msg.ToString();

            static string FormatIO(IDictionary<string, BitsValue> values)
            {
                return string.Join(' ', values.OrderBy(e => e.Key).Select(e => $"{e.Key}({e.Value.Number})").ToArray());
            }
        }
    }
}