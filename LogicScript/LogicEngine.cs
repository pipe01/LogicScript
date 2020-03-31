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
            if (c.Statements != null
                && c.InputSpec is CompoundInputSpec inputSpec
                && AreInputsMatched(machine, inputSpec.Indices, c.InputsValue.Values))
            {
                foreach (var stmt in c.Statements)
                {
                    RunStatement(machine, stmt);
                }
            }
        }

        private static bool AreInputsMatched(IMachine machine, int[] inputIndices, BitValue[] values)
        {
            bool match = true;

            for (int i = 0; i < inputIndices.Length; i++)
            {
                bool inputValue = machine.GetInput(inputIndices[i]);
                bool requiredValue = GetBitValue(machine, values[i]);

                if (inputValue != requiredValue)
                {
                    match = false;
                    break;
                }
            }

            return match;
        }

        private static void RunStatement(IMachine machine, Statement stmt)
        {
            if (stmt is OutputSetStatement outset)
            {
                if (outset.Output.Index == null)
                {
                    Span<bool> values = stackalloc bool[outset.Value.Values.Length];

                    for (int i = 0; i < outset.Value.Values.Length; i++)
                    {
                        values[i] = GetBitValue(machine, outset.Value.Values[i]);
                    }

                    machine.SetOutputs(values);
                }
                else
                {
                    machine.SetOutput(outset.Output.Index.Value, GetBitValue(machine, outset.Value.Values[0]));
                }
            }
        }

        private static bool GetBitValue(IMachine machine, BitValue bit)
        {
            if (bit is LiteralBitValue l)
            {
                return l.Value;
            }
            else if (bit is InputBitValue i)
            {
                return machine.GetInput(i.InputIndex);
            }

            throw new Exception("Invalid type");
        }
    }
}
