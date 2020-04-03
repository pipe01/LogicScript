using LogicScript.Data;
using LogicScript.Parsing.Structures;
using System;

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

            var conditionValue = GetValue(machine, c.Condition);
            var value = GetValue(machine, c.InputsValue);

            if (conditionValue == value)
                RunStatements(machine, c.Statements);
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
                var value = GetValue(machine, setsingle.Value);

                if (!value.IsSingleBit)
                    throw new LogicEngineException("Expected single-bit value", setsingle.Value);

                machine.SetOutput(setsingle.Output, value);
            }
            else if (stmt is SetOutputStatement setout)
            {
                machine.SetOutputs(GetValue(machine, setout.Value));
            }
        }

        private static BitsValue GetValue(IMachine machine, Expression expr)
        {
            switch (expr)
            {
                case NumberLiteralExpression num when num.Length == 1:
                    return num.Value == 1;
                case NumberLiteralExpression num:
                    return new BitsValue(num.Value, num.Length);
                case ListExpression list:
                    return GetListValue(list);
                case BitwiseOperator op:
                    return DoBitsOperator(machine, op);
                case WholeInputExpression _:
                    return machine.GetInputs();
                case SingleInputExpression input:
                    return machine.GetInput(input.InputIndex);

                default:
                    throw new LogicEngineException("Expected multi-bit value", expr);
            }

            BitsValue GetListValue(ListExpression list)
            {
                var items = new bool[list.Expressions.Length];
                ulong n = 0;

                int len = list.Expressions.Length;
                for (int i = 0; i < len; i++)
                {
                    var value = GetValue(machine, list.Expressions[i]);

                    if (!value.IsSingleBit)
                        throw new LogicEngineException("List expressions can only contain single-bit values", list.Expressions[i]);

                    if (value)
                        n |= 1UL << (len - 1 - i);
                }

                return new BitsValue(items);
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
                    var value = GetValue(machine, expr);

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
