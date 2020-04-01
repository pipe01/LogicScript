using LogicScript.Parsing.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicScript
{
    public sealed class LogicEngine
    {
        private readonly Script Script;

        public LogicEngine(Script script)
        {
            this.Script = script ?? throw new ArgumentNullException(nameof(script));
        }

        public void DoUpdate(IMachine machine)
        {
            foreach (var item in Script.Cases)
            {
                UpdateCase(machine, item);
            }
        }

        private static void UpdateCase(IMachine machine, Case c)
        {
            if (c.Statements != null)
            {
                if (c.InputSpec is CompoundInputSpec inputSpec)
                {
                    if (AreInputsMatched(machine, c.InputsValue.Values, inputSpec.Indices))
                        RunStatements(machine, c.Statements);
                }
                else if (c.InputSpec is WholeInputSpec)
                {
                    if (c.InputsValue.Values.Length != machine.InputCount)
                        throw new LogicEngineException("Mismatched input count", c);

                    if (AreInputsMatched(machine, c.InputsValue.Values))
                        RunStatements(machine, c.Statements);
                }
            }
        }

        private static bool AreInputsMatched(IMachine machine, BitExpression[] values, int[]? inputIndices = null)
        {
            bool match = true;
            bool wholeInput = inputIndices == null;

            int size = wholeInput ? machine.InputCount : inputIndices!.Length;

            for (int i = 0; i < size; i++)
            {
                bool inputValue = machine.GetInput(wholeInput ? i : inputIndices![i]);
                bool requiredValue = GetBitValue(machine, values[i]);

                if (inputValue != requiredValue)
                {
                    match = false;
                    break;
                }
            }

            return match;
        }

        private static void RunStatements(IMachine machine, IEnumerable<Statement> statements)
        {
            foreach (var stmt in statements)
            {
                RunStatement(machine, stmt);
            }
        }

        private static void RunStatement(IMachine machine, Statement stmt)
        {
            if (stmt is OutputSetStatement outset)
            {
                if (outset.Output.Index == null)
                {
                    if (!(outset.Value is BitsValue bits))
                        throw new LogicEngineException("Expected bits value", stmt);

                    Span<bool> values = stackalloc bool[bits.Values.Length];

                    for (int i = 0; i < bits.Values.Length; i++)
                    {
                        values[i] = GetBitValue(machine, bits.Values[i]);
                    }

                    machine.SetOutputs(values);
                }
                else
                {
                    if (!(outset.Value is BitExpression bit))
                        throw new LogicEngineException("Expected bit value", stmt);

                    machine.SetOutput(outset.Output.Index.Value, GetBitValue(machine, bit));
                }
            }
        }

        private static bool GetBitValue(IMachine machine, BitExpression bit)
        {
            if (bit is LiteralBitExpression l)
            {
                return l.Value;
            }
            else if (bit is InputBitExpression i)
            {
                return machine.GetInput(i.InputIndex);
            }

            throw new Exception("Invalid type");
        }
    }
}
