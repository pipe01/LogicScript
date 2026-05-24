using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Interpreting.Debugging;
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

        public async Task<CaseResult> Run(Script script, IDebugger? debugger)
        {
            var machine = new TestingMachine(script.RegisteredInputLength, script.RegisteredOutputLength);

            return await Run(script, machine, debugger);
        }

        internal async Task<CaseResult> Run(Script script, TestingMachine machine, IDebugger? debugger)
        {
            var hasRunStartup = false;
            int stepsRan = 0;

            CaseStep? failedStep = null;
            var mismatchedOutputs = new Dictionary<string, BitsValue>();

            foreach (var step in Steps)
            {
                foreach (var input in step.Inputs)
                {
                    if (!script.Inputs.TryGetValue(input.Name, out var port))
                        throw new ArgumentException($"Unknown input port '{input.Name}'");

                    Span<bool> values = stackalloc bool[port.BitSize];
                    input.Value.FillBits(values);
                    values.Reverse();

                    values.CopyTo(machine.Inputs.AsSpan()[port.StartIndex..]);
                }

                await new Interpreter(script, machine, !hasRunStartup, debugger: debugger).RunToEndAsync();
                hasRunStartup = true;
                stepsRan++;

                foreach (var output in step.Outputs)
                {
                    if (!script.Outputs.TryGetValue(output.Name, out var port))
                        throw new ArgumentException($"Unknown output port '{output.Name}'");

                    Span<bool> values = stackalloc bool[port.BitSize];
                    machine.Outputs[port.StartIndex..].CopyTo(values);
                    values.Reverse();

                    var machineValue = new BitsValue(values);

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

            var printed = machine.PrintOutput.ToArray();

            if (failedStep == null)
            {
                return new CaseResult(Index, Name, Steps.Count, printed);
            }

            var resultInputs = failedStep.Inputs.ToDictionary(o => o.Name, o => o.Value);
            var resultOutputs = failedStep.Outputs.ToDictionary(o => o.Name, o => o.Value);

            return new CaseResult(Index, Name, Steps.Count, printed, new(stepsRan, resultInputs, resultOutputs, mismatchedOutputs));
        }
    }
}