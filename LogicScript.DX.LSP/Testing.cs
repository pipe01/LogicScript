using System.Text;
using LogicScript.Testing;
using LogicScript.Testing.Results;

namespace LogicScript.DX.LSP
{
    public static class Testing
    {
        public static string FormatResult(CaseResult result, TestCase testCase, bool appendOutput = true)
        {
            var message = new StringBuilder();
            message.AppendLine($"* At {testCase.Span.Start.FileName}:{testCase.Span.Start}");
            message.Append($"** Test \"{testCase.Name ?? testCase.Index.ToString()}\" ");

            if (result.Success)
                message.AppendLine("succeeded");
            else
                message.AppendLine("failed:");

            switch (result)
            {
                case FailedStepCaseResult failedStep:
                    message.Append(failedStep.GetFailureString());
                    break;

                case LimitReachedCaseResult:
                    message.AppendLine("Statement limit reached");
                    message.AppendLine("Check your code for any infinite loops or try raising the statement limit in the extension's settings.");
                    break;
            }

            if (appendOutput && result.PrintedLines.Count > 0)
            {
                message.AppendLine("\nOutput:");

                foreach (var line in result.PrintedLines)
                {
                    message.AppendLine("  " + line);
                }
            }

            return message.ToString();
        }
    }
}