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

        public async Task<CaseResult> Run(Script script, IDebugger? debugger, int statementLimit)
        {
            var machine = new TestingMachine(script.RegisteredInputLength, script.RegisteredOutputLength);

            return await Run(script, machine, debugger, statementLimit);
        }

        internal async Task<CaseResult> Run(Script script, TestingMachine machine, IDebugger? debugger, int statementLimit)
        {
            var hasRunStartup = false;
            int stepsRan = 0;

            foreach (var step in Steps)
            {
                foreach (var input in step.Inputs)
                {
                    if (!script.Inputs.TryGetValue(input.Name, out var port))
                        throw new ArgumentException($"Unknown input port '{input.Name}'");

                    var expandedValue = new BitsValue(input.Value.Number, port.BitSize);

                    expandedValue.Bits.CopyTo(machine.Inputs.AsSpan()[port.StartIndex..(port.StartIndex + port.BitSize)]);
                }

                var exitReason = await new Interpreter(script, machine, !hasRunStartup, debugger: debugger).RunToEndAsync(statementLimit);
                if (exitReason != ExitReason.Ended)
                {
                    if (exitReason == ExitReason.LimitReached)
                    {
                        return new LimitReachedCaseResult(this, [.. machine.PrintOutput], stepsRan);
                    }
                }

                hasRunStartup = true;
                stepsRan++;

                var mismatchedOutputs = new Dictionary<string, BitsValue>();
                foreach (var output in step.Outputs)
                {
                    if (!script.Outputs.TryGetValue(output.Name, out var port))
                        throw new ArgumentException($"Unknown output port '{output.Name}'");

                    var machineValue = new BitsValue(machine.Outputs[port.StartIndex..(port.StartIndex + port.BitSize)]);

                    var expandedValue = new BitsValue(output.Value.Number, port.BitSize);
                    if (output.Value != machineValue)
                    {
                        mismatchedOutputs[output.Name] = machineValue;
                    }
                }

                if (mismatchedOutputs.Count > 0)
                {
                    var resultInputs = step.Inputs.ToDictionary(o => o.Name, o => o.Value);
                    var resultOutputs = step.Outputs.ToDictionary(o => o.Name, o => o.Value);

                    return new FailedStepCaseResult(this, [.. machine.PrintOutput], stepsRan, step.Span, resultInputs, resultOutputs, mismatchedOutputs);
                }
            }

            return new SuccessStepResult(this, [.. machine.PrintOutput]);
        }
    }
}