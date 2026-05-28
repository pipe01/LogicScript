using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Testing.Results;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;

namespace LogicScript.Testing
{
    public readonly record struct TestCase(int Index, string? Name, IReadOnlyList<CaseStep> Steps, SourceSpan Span) : ICodeNode
    {
        public IEnumerable<ICodeNode> GetChildren()
        {
            return Steps;
        }

        public async Task<CaseResult> Run(Runner runner, Script script)
        {
            var machine = new TestingMachine(script.RegisteredInputLength, script.RegisteredOutputLength);

            return await Run(runner, script, machine);
        }

        internal async Task<CaseResult> Run(Runner runner, Script script, TestingMachine machine)
        {
            var hasRunStartup = false;
            int stepsRan = 0;

            foreach (var step in Steps)
            {
                foreach (var input in step.Inputs)
                {
                    if (!script.Inputs.TryGetValue(input.Name, out var port))
                        throw new ArgumentException($"Unknown input port '{input.Name}'");

                    for (int i = 0; i < input.Values.Length; i++)
                    {
                        int vectorStart = port.StartIndex + i * port.BitSize;
                        var expandedValue = new BitsValue(input.Values[i].Value.Number, port.BitSize);

                        expandedValue.Bits.CopyTo(machine.Inputs.AsSpan()[vectorStart..(vectorStart + port.BitSize)]);
                    }
                }

                try
                {
                    if (runner.CanRunAsync)
                        await runner.RunAsync(machine, script, !hasRunStartup);
                    else
                        runner.Run(machine, script, !hasRunStartup);
                }
                catch (InterpreterLimitReachedException)
                {
                    return new LimitReachedCaseResult(this, [.. machine.PrintOutput], stepsRan);
                }

                hasRunStartup = true;
                stepsRan++;

                var mismatchedOutputs = new Dictionary<string, BitsValue[]>();
                foreach (var output in step.Outputs)
                {
                    if (!script.Outputs.TryGetValue(output.Name, out var port))
                        throw new ArgumentException($"Unknown output port '{output.Name}'");

                    var machineValues = Enumerable.Range(0, output.Values.Length).Select(i =>
                    {
                        int vectorStart = port.StartIndex + i * port.BitSize;
                        return new BitsValue(machine.Outputs[vectorStart..(vectorStart + port.BitSize)]);
                    });

                    bool mismatched = !machineValues.SequenceEqual(output.Values.Select(v => v.Value));

                    if (mismatched)
                        mismatchedOutputs.Add(output.Name, machineValues.ToArray());
                }

                if (mismatchedOutputs.Count > 0)
                {
                    var resultInputs = step.Inputs.ToDictionary(o => o.Name, o => o.Values.Select(v => v.Value).ToArray());
                    var resultOutputs = step.Outputs.ToDictionary(o => o.Name, o => o.Values.Select(v => v.Value).ToArray());

                    return new FailedStepCaseResult(this, [.. machine.PrintOutput], stepsRan, step.Span, resultInputs, resultOutputs, mismatchedOutputs);
                }
            }

            return new SuccessStepResult(this, [.. machine.PrintOutput]);
        }
    }
}