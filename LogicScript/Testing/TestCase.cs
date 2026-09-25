using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using LogicScript.Data;
using LogicScript.Testing.Results;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using System.Threading;
using LogicScript.Compiling;

namespace LogicScript.Testing
{
    public readonly record struct TestCase(int Index, string? Name, IReadOnlyList<CaseStep> Steps, SourceSpan Span) : ICodeNode
    {
        public IEnumerable<ICodeNode> GetChildren()
        {
            return Steps;
        }

        public async Task<CaseResult> Run(ICompiledScript compiledScript, Script script, IDebugger? debugger = null, CancellationToken cancellationToken = default)
        {
            var machine = new TestingMachine(script.RegisteredInputLength, script.RegisteredOutputLength);

            if (debugger != null)
                machine.LineOutput += debugger.GotOutput;

            var instance = compiledScript.Instantiate(machine);
            instance.Debugger = debugger;

            return await Run(instance, script, machine, cancellationToken);
        }

        internal async Task<CaseResult> Run(IScriptInstance instance, Script script, TestingMachine machine, CancellationToken cancellationToken = default)
        {
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

                await Task.Factory.StartNew(instance.Run, TaskCreationOptions.LongRunning);

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
                    var resultOutputs = step.Outputs.ToDictionary(o => o.Name, o => o.Values.Select(v => v.Value).ToArray());

                    return new FailedStepCaseResult(this, [.. machine.PrintOutput], stepsRan, step, step.Span.GetText(script.Source), resultOutputs, mismatchedOutputs);
                }
            }

            return new SuccessStepResult(this, [.. machine.PrintOutput]);
        }
    }
}