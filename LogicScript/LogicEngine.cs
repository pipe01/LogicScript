using LogicScript.Data;
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
            if (c.Statements == null)
                return;

            var value = GetBitsValue(machine, c.InputsValue);

            if (c.InputSpec is CompoundInputSpec compound)
            {
                if (value.Length != compound.Indices.Length)
                    throw new LogicEngineException("Mismatched input count", c);

                if (AreInputsMatched(machine, compound.Indices, value))
                    RunStatements(machine, c.Statements);
            }
            else if (c.InputSpec is WholeInputSpec)
            {
                if (value.Length != machine.InputCount)
                    throw new LogicEngineException("Mismatched input count", c);

                //if (c.InputsValue.Values.Length != machine.InputCount)
                //    throw new LogicEngineException("Mismatched input count", c);

                //if (AreInputsMatched(machine, c.InputsValue.Values))
                //    RunStatements(machine, c.Statements);
            }
        }

        private static bool AreInputsMatched(IMachine machine, int[] inputIndices, BitsValue bits)
        {
            bool match = true;

            for (int i = 0; i < inputIndices.Length; i++)
            {
                bool inputValue = machine.GetInput(i);
                bool requiredValue = bits[i];

                if (inputValue != requiredValue)
                {
                    match = false;
                    break;
                }
            }

            return match;
        }
        
        private static bool AreInputsMatched(IMachine machine, BitsValue bits)
        {
            bool match = true;

            for (int i = 0; i < machine.InputCount; i++)
            {
                bool inputValue = machine.GetInput(i);
                bool requiredValue = bits[i];

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
                    machine.SetOutputs(GetBitsValue(machine, outset.Value));
                }
                else
                {
                    machine.SetOutput(outset.Output.Index.Value, GetBitValue(machine, outset.Value));
                }
            }
        }

        private static bool GetBitValue(IMachine machine, Expression expr)
        {
            switch (expr)
            {
                case NumberLiteralExpression num when num.Length == 1:
                    return num.Value == 1;
                case InputExpression input:
                    return machine.GetInput(input.InputIndex);

                default:
                    throw new LogicEngineException("Expected single-bit value", expr);
            }
        }

        private static BitsValue GetBitsValue(IMachine machine, Expression expr)
        {
            switch (expr)
            {
                case NumberLiteralExpression num:
                    return new BitsValue(num.Value, num.Length);
                case ListExpression list:
                    return GetListValue(list);

                default:
                    throw new LogicEngineException("Expected multi-bit value", expr);
            }

            BitsValue GetListValue(ListExpression list)
            {
                var items = new bool[list.Expressions.Length];

                for (int i = 0; i < list.Expressions.Length; i++)
                {
                    items[i] = GetBitValue(machine, list.Expressions[i]);
                }

                return new BitsValue(items);
            }
        }
    }
}
