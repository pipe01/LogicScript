using Antlr4.Runtime.Misc;
using LogicScript.Parsing.Structures;
using LogicScript.Testing;

namespace LogicScript.Parsing.Visitors
{
    internal class ScriptVisitor(ErrorSink errors, string source) : LogicScriptParserBaseVisitor<Script>
    {
        public override Script VisitScript([NotNull] LogicScriptParser.ScriptContext context)
        {
            var script = new Script(source, context.Start.TokenSource.SourceName, errors);
            var ctx = new ScriptContext(script, errors);

            new FunctionDeclarationVisitor(ctx).Visit(context);

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
                            errors.AddUnknownInputPort(input.Name, input.NameSpan);
                        else
                            CheckTestPort(inputPort, input);
                    }
                    foreach (var output in step.Outputs)
                    {
                        if (!script.Outputs.TryGetValue(output.Name, out var outputPort))
                            errors.AddUnknownOutputPort(output.Name, output.NameSpan);
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
                    errors.AddPortVectorTooShort(values.ValuesSpan);
                else if (values.Values.Length > port.VectorLength)
                    errors.AddPortVectorTooLong(values.ValuesSpan);

                foreach (var value in values.Values)
                {
                    if (value.Value.Length > port.BitSize)
                        errors.AddPortValueTooLarge(value.Span);
                }
            }
        }
    }
}
