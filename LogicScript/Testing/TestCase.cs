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

        public async Task<CaseResult> Run(Script script, IDebugger? debugger = null, CancellationToken cancellationToken = default)
        {
            var machine = new TestingMachine(script.RegisteredInputLength, script.RegisteredOutputLength);

            if (debugger != null)
                machine.LineOutput += debugger.GotOutput;

            var instance = script.CreateInstance(machine, debugger != null);
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
                    if (!script.Inputs.TryGetValue(input.Name, out var port) && !script.Registers.TryGetValue(input.Name, out port))
                        throw new ArgumentException($"Unknown input port '{input.Name}'");

                    int offset = (int)(input.Offset ?? 0);

                    for (int i = 0; i < input.Values.Length; i++)
                    {
                        var portValue = input.Values[i];
                        ulong value = portValue.Value.Number;

                        if (port.Target == MachinePorts.Register)
                        {
                            instance.SetRegister(port.StartIndex, i + offset, value);
                        }
                        else
                        {
                            int vectorStart = port.StartIndex + (i + offset) * port.BitSize;
                            var expandedValue = new BitsValue(value, port.BitSize);
                            expandedValue.Bits.CopyTo(machine.Inputs.AsSpan()[vectorStart..(vectorStart + port.BitSize)]);
                        }
                    }
                }

                await Task.Factory.StartNew(instance.Run, TaskCreationOptions.LongRunning);

                stepsRan++;

                var mismatchedOutputs = new Dictionary<MachinePortInfo, BitsValue[]>();
                var resultOutputs = new Dictionary<MachinePortInfo, BitsValue[]>();

                foreach (var assertion in step.Assertions)
                {
                    if (!script.Outputs.TryGetValue(assertion.Name, out var port) && !script.Registers.TryGetValue(assertion.Name, out port))
                        throw new ArgumentException($"Unknown output port '{assertion.Name}'");

                    int offset = (int)(assertion.Offset ?? 0);

                    var gotValues = Enumerable.Range(0, assertion.Values.Length).Select(i =>
                    {

                        if (port.Target == MachinePorts.Output)
                        {
                            int vectorStart = port.StartIndex + (i + offset) * port.BitSize;
                            return new BitsValue(machine.Outputs[vectorStart..(vectorStart + port.BitSize)]);
                        }
                        else
                        {
                            return new BitsValue(instance.GetRegister(port.StartIndex, i + offset));
                        }
                    }).ToArray();

                    resultOutputs.Add(port, [.. assertion.Values.Select(v => v.Value)]);

                    bool mismatched = !gotValues.SequenceEqual(assertion.Values.Select(v => v.Value));

                    if (mismatched)
                        mismatchedOutputs.Add(port, gotValues);
                }

                if (mismatchedOutputs.Count > 0)
                {
                    return new FailedStepCaseResult(this, [.. machine.PrintOutput], stepsRan, step, step.Span.GetText(script.Source), resultOutputs, mismatchedOutputs);
                }
            }

            return new SuccessStepResult(this, [.. machine.PrintOutput]);
        }
    }
}