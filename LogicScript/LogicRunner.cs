using LogicScript.Data;
using LogicScript.Parsing.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicScript
{
    public sealed class LogicRunner
    {
        private readonly Script Script;

        public LogicRunner(Script script)
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
            bool match = false;

            if (c.InputSpec is CompoundInputSpec compound)
            {
                match = AreInputsMatched(machine, value, compound.Indices);
            }
            else if (c.InputSpec is WholeInputSpec)
            {
                match = AreInputsMatched(machine, value);
            }
            else if (c.InputSpec is SingleInputSpec singlein)
            {
                if (!value.IsSingleBit)
                    throw new LogicEngineException("Mismatched input count, expected 1", c);

                match = machine.GetInput(singlein.Index) == value[0];
            }

            if (match)
                RunStatements(machine, c.Statements);
        }

        private static bool AreInputsMatched(IMachine machine, BitsValue bits, int[] inputIndices = null)
        {
            bool match = true;
            int length = inputIndices?.Length ?? machine.InputCount;

            for (int i = 0; i < length; i++)
            {
                bool inputValue = machine.GetInput(inputIndices?[i] ?? i);
                bool requiredValue = bits[i];

                if (inputValue != requiredValue)
                {
                    match = false;
                    break;
                }
            }

            return match;
        }

        private static void RunStatements(IMachine machine, Statement[] statements)
        {
            for (int i = 0; i < statements.Length; i++)
            {
                RunStatement(machine, statements[i]);
            }
        }

        private static void RunStatement(IMachine machine, Statement stmt)
        {
            if (stmt is SetSingleOutputStatement setsingle)
            {
                machine.SetOutput(setsingle.Output, GetBitValue(machine, setsingle.Value));
            }
            else if (stmt is SetOutputStatement setout)
            {
                machine.SetOutputs(GetBitsValue(machine, setout.Value));
            }
        }

        private static bool GetBitValue(IMachine machine, Expression expr)
        {
            switch (expr)
            {
                case NumberLiteralExpression num when num.Length == 1:
                    return num.Value == 1;
                case SingleInputExpression input:
                    return machine.GetInput(input.InputIndex);
                case BitwiseOperator op:
                    return DoBitOperator(machine, op);

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
                case BitwiseOperator op:
                    return DoBitsOperator(machine, op);
                case WholeInputExpression _:
                    return machine.GetInputs();

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

        private static bool DoBitOperator(IMachine machine, BitwiseOperator op)
        {
            switch (op.Operator)
            {
                case Operator.And:
                    return op.Operands.Length == 1
                        ? GetBitsValue(machine, op.Operands[0]).AggregateBits(true, (a, b) => a && b, false)
                        : Aggregate(true, (a, b) => a && b, false);
                case Operator.Or:
                    return Aggregate(false, (a, b) => a || b, true);
            }

            throw new LogicEngineException();

            bool Aggregate(bool start, Func<bool, bool, bool> aggregator, bool? shortCircuitOn = null)
            {
                bool val = start;

                foreach (var item in op.Operands)
                {
                    val = aggregator(val, GetBitValue(machine, item));

                    if (val == shortCircuitOn)
                        break;
                }

                return val;
            }
        }

        private static BitsValue DoBitsOperator(IMachine machine, BitwiseOperator op)
        {
            switch (op.Operator)
            {
                case Operator.And:
                    return Aggregate((a, b) => a & b, o => o == BitsValue.Zero);
                case Operator.Or:
                    return Aggregate((a, b) => a | b);
            }

            throw new LogicEngineException();

            BitsValue Aggregate(Func<BitsValue, BitsValue, BitsValue> aggregator, Func<BitsValue, bool> shortCircuit = null)
            {
                BitsValue? curVal = null;

                foreach (var expr in op.Operands)
                {
                    var value = GetBitsValue(machine, expr);

                    if (curVal == null)
                        curVal = value;
                    else
                        curVal = aggregator(curVal.Value, value);

                    if (shortCircuit != null && shortCircuit(curVal.Value))
                        break;
                }

                return curVal.Value;
            }
        }
    }
}
