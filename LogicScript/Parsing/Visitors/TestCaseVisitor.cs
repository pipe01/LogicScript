using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using LogicScript.Parsing.Structures;
using LogicScript.Testing;

namespace LogicScript.Parsing.Visitors
{
    internal class TestCaseVisitor(int index, ErrorSink errors) : LogicScriptBaseVisitor<TestCase>
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
                        errors.AddError("The first step on a case must not be a repetition", repeat.Span());
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
                        errors.AddError("Missing outputs declaration", action.Span());
                        continue;
                    }

                    var inputs = GetPorts(MachinePorts.Input, action.inputs).ToList();
                    var outputs = GetPorts(MachinePorts.Output, action.outputs).ToList();

                    lastStep = new CaseStep(inputs, outputs, step.Span());
                    steps.Add(lastStep);
                }
            }

            return new(index++, name, steps, context.Span());

            IEnumerable<PortValue> GetPorts(MachinePorts ports, LogicScriptParser.Step_portsContext ctx)
            {
                var seen = new HashSet<string>();

                foreach (var item in ctx.step_portvalue())
                {
                    if (item.value == null)
                    {
                        errors.AddError("Missing port value", item.Span());
                        continue;
                    }

                    if (seen.Contains(item.port.Text))
                    {
                        errors.AddError("Duplicate port", item.Span());
                        continue;
                    }
                    seen.Add(item.port.Text);

                    var value = new NumberVisitor().Visit(item.value);
                    yield return new PortValue(item.port.Text, ports, value, item.port.Span(), item.value.Span());
                }
            }
        }
    }
}