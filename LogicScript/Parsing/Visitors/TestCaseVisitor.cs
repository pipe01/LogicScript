using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using LogicScript.Parsing.Structures;
using LogicScript.Testing;

namespace LogicScript.Parsing.Visitors
{
    internal class TestCaseVisitor(ScriptContext? script, int index, ErrorSink errors) : LogicScriptParserBaseVisitor<TestCase>
    {
        public override TestCase VisitTest_case([NotNull] LogicScriptParser.Test_caseContext context)
        {
            var name = context.name?.Text.Trim('"');
            var steps = new List<CaseStep>();
            CaseStep? lastStep = null;

            foreach (var step in context.test_step())
            {
                var repeat = step.step_repeat();
                if (repeat != null)
                {
                    if (lastStep == null)
                    {
                        errors.AddFirstStepCannotRepeat(repeat.Span());
                        continue;
                    }

                    for (int i = 0; i < int.Parse(repeat.DEC_NUMBER().GetText()); i++)
                    {
                        steps.Add(lastStep);
                    }
                }
                else
                {
                    var action = step.step_action();

                    if (action.outputs == null)
                    {
                        errors.AddOutputsMissing(action.Span());
                        continue;
                    }

                    var inputs = GetPorts(MachinePorts.Input, action.inputs).ToList();
                    var outputs = GetPorts(MachinePorts.Output, action.outputs).ToList();

                    lastStep = new CaseStep(inputs, outputs, step.Span());
                    steps.Add(lastStep);
                }
            }

            return new(index++, name, steps, context.Span());

            IEnumerable<PortValues> GetPorts(MachinePorts ports, LogicScriptParser.Step_portsContext ctx)
            {
                var seen = new HashSet<string>();

                foreach (var item in ctx.step_portvalue())
                {
                    if (item.expression() == null || item.expression().Length == 0)
                    {
                        errors.AddPortValueMissing(item.Span());
                        continue;
                    }

                    if (seen.Contains(item.port.Text))
                    {
                        errors.AddDuplicatePort(item.Span());
                        continue;
                    }
                    seen.Add(item.port.Text);

                    var values = item.expression().Select(e => new PortValue(e.GetConstantValue(script ?? new(new(), errors)), e.Span())).ToArray();
                    yield return new PortValues(ctx.Span(), item.port.Text, ports, values, item.port.Span());
                }
            }
        }
    }
}