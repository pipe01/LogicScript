using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using LogicScript.Data;
using LogicScript.Parsing;
using LogicScript.Parsing.Visitors;
using LogicScript.Testing.Results;

namespace LogicScript.Testing
{
    public record struct TestResults(bool Success, IReadOnlyList<CaseResult> Cases);

    public class TestBench
    {
        private readonly IList<TestCase> Cases;
        private readonly Script Script;

        public int CaseCount => Cases.Count;

        internal TestBench(IList<TestCase> cases, Script script)
        {
            this.Cases = cases;
            this.Script = script;
        }

        public IEnumerable<CaseResult> Run()
        {
            foreach (var @case in Cases)
            {
                yield return RunCase(@case);
            }
        }

        private CaseResult RunCase(TestCase @case)
        {
            var machine = new TestingMachine(Script.RegisteredInputLength, Script.RegisteredOutputLength);
            var hasRunStartup = false;
            int stepsRan = 0;

            CaseStep? failedStep = null;
            var mismatchedOutputs = new Dictionary<string, BitsValue>();

            foreach (var step in @case.Steps)
            {
                foreach (var input in step.Inputs)
                {
                    input.Value.FillBits(machine.Inputs.AsSpan()[input.Port.StartIndex..(input.Port.StartIndex + input.Port.BitSize)]);
                }

                Script.Run(machine, !hasRunStartup);
                hasRunStartup = true;
                stepsRan++;

                foreach (var output in step.Outputs)
                {
                    var machineValue = new BitsValue(machine.Outputs[output.Port.StartIndex..(output.Port.StartIndex + output.Port.BitSize)]);

                    if (output.Value != machineValue)
                    {
                        mismatchedOutputs[output.Name] = machineValue;

                        failedStep = step;
                    }
                }

                if (failedStep != null)
                {
                    break;
                }
            }

            if (failedStep == null)
            {
                return new CaseResult(@case.Name, @case.Steps.Count);
            }

            var resultInputs = failedStep.Inputs.ToDictionary(o => o.Name, o => o.Value);
            var resultOutputs = failedStep.Outputs.ToDictionary(o => o.Name, o => o.Value);

            return new CaseResult(@case.Name, @case.Steps.Count, new FailedStep(stepsRan, resultInputs, resultOutputs, mismatchedOutputs));
        }

        internal static TestBench FromParsed(LogicScriptParser.Test_benchContext ctx, Script script)
        {
            var cases = new List<TestCase>();

            foreach (var testCase in ctx.test_case())
            {
                var name = testCase.name.Text.Trim('"');
                var steps = new List<CaseStep>();
                CaseStep? lastStep = null;

                foreach (var step in testCase.test_step())
                {
                    var repeat = step.step_repeat();
                    if (repeat != null)
                    {
                        if (lastStep == null)
                            throw new ParseException("The first step on a case must not be a repetition", step.Span());
                        
                        steps.Add(lastStep);
                    }
                    else
                    {
                        var action = step.step_action();
                        var inputs = GetPorts(action.inputs, script.Inputs).ToList();
                        var outputs = GetPorts(action.outputs, script.Outputs).ToList();

                        steps.Add(new CaseStep(inputs, outputs));
                    }
                }

                cases.Add(new TestCase(name, steps));

                static IEnumerable<PortValue> GetPorts(LogicScriptParser.Step_portsContext ctx, IDictionary<string, Parsing.Structures.PortInfo> scriptPorts)
                {
                    foreach (var item in ctx.step_portvalue())
                    {
                        if (!scriptPorts.TryGetValue(item.port.Text, out var port))
                            throw new ParseException($"Unknown input {item.port.Text}", item.Span());

                        var value = new NumberVisitor().Visit(item.value);
                        yield return new PortValue(item.port.Text, port, value);
                    }
                }
            }

            return new TestBench(cases, script);
        }

        public static (TestBench? Bench, IReadOnlyList<Error> Errors) Parse(string source, Script script)
        {
            var errors = new ErrorSink();

            var input = new AntlrInputStream(source.Replace("\r\n", "\n") + "\n");
            var lexer = new LogicScriptLexer(input);
            var stream = new CommonTokenStream(lexer);
            var parser = new LogicScriptParser(stream);
            parser.AddErrorListener(new ErrorListener(errors));

            return (FromParsed(parser.test_bench(), script), Array.Empty<Error>());
        }
    }
}