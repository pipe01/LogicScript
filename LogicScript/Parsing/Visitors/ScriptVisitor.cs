using Antlr4.Runtime.Misc;
using LogicScript.Parsing.Structures;
using LogicScript.Testing;

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
                        if (!script.Inputs.TryGetValue(input.Name, out var inputPort))
                            errors.AddError($"Unknown input port '{input.Name}'", input.NameSpan);
                        else
                            CheckTestPort(inputPort, input);
                    }
                    foreach (var output in step.Outputs)
                    {
                        if (!script.Outputs.TryGetValue(output.Name, out var outputPort))
                            errors.AddError($"Unknown output port '{output.Name}'", output.NameSpan);
                        else
                            CheckTestPort(outputPort, output);
                    }
                }

                script.TestCases.Add(testCase);
            }

            return script;

            void CheckTestPort(MachinePortInfo port, PortValues values)
            {
                if (values.Values.Length < port.VectorLength)
                    errors.AddError("Not enough values to fill port vector", values.ValuesSpan);
                else if (values.Values.Length > port.VectorLength)
                    errors.AddError("Too many values for port vector", values.ValuesSpan);

                foreach (var value in values.Values)
                {
                    if (value.Value.Length > port.BitSize)
                        errors.AddError("Value doesn't fit in port", value.Span);
                }
            }
        }
    }
}
