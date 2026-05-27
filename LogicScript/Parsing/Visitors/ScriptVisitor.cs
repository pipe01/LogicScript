using Antlr4.Runtime.Misc;

namespace LogicScript.Parsing.Visitors
{
    internal class ScriptVisitor(ErrorSink errors, string source) : LogicScriptBaseVisitor<Script>
    {
        public override Script VisitScript([NotNull] LogicScriptParser.ScriptContext context)
        {
            var script = new Script(source, context.Start.TokenSource.SourceName, errors);
            var ctx = new ScriptContext(script, errors);

            var declVisitor = new DeclarationVisitor(ctx, errors);
            foreach (var decl in context.declaration())
            {
                declVisitor.Visit(decl);
            }

            int caseCounter = 0;
            foreach (var testCaseCtx in context.test_case())
            {
                var testCaseVisitor = new TestCaseVisitor(ctx, caseCounter++, errors);
                var testCase = testCaseVisitor.VisitTest_case(testCaseCtx);

                foreach (var step in testCase.Steps)
                {
                    foreach (var input in step.Inputs)
                    {
                        if (!script.Inputs.TryGetValue(input.Name, out var stepInput))
                            errors.AddError($"Unknown input port '{input.Name}'", input.NameSpan);
                        else if (stepInput.BitSize < input.Value.Length)
                            errors.AddError("Value doesn't fit in port", input.ValueSpan);
                    }
                    foreach (var output in step.Outputs)
                    {
                        if (!script.Outputs.TryGetValue(output.Name, out var stepOutput))
                            errors.AddError($"Unknown output port '{output.Name}'", output.NameSpan);
                        else if (stepOutput.BitSize < output.Value.Length)
                            errors.AddError("Value doesn't fit in port", output.ValueSpan);
                    }
                }

                script.TestCases.Add(testCase);
            }

            return script;
        }
    }
}
