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
                        errors.AddError("The first step on a case must not be a repetition", step.Span());
                        continue;
                    }

                    steps.Add(lastStep);
                }
                else
                {
                    var action = step.step_action();
                    var inputs = GetPorts(MachinePorts.Input, action.inputs).ToList();
                    var outputs = GetPorts(MachinePorts.Output, action.outputs).ToList();

                    steps.Add(new CaseStep(inputs, outputs, step.Span()));
                }
            }

            return new(index++, name, steps, context.Span());

            static IEnumerable<PortValue> GetPorts(MachinePorts ports, LogicScriptParser.Step_portsContext ctx)
            {
                foreach (var item in ctx.step_portvalue())
                {
                    var value = new NumberVisitor().Visit(item.value);
                    yield return new PortValue(item.port.Text, ports, value, item.port.Span(), item.value.Span());
                }
            }
        }
    }
}