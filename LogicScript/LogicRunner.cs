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

        public void DoUpdate(IMachine machine, bool isFirstUpdate = false)
        {
            int len = Script.Cases.Count;
            for (int i = 0; i < len; i++)
            {
                UpdateCase(machine, Script.Cases[i], isFirstUpdate);
            }
        }

        private static void UpdateCase(IMachine machine, Case c, bool firstUpdate)
        {
            if (c.Statements == null)
                return;

            bool run = false;

            switch (c)
            {
                case ConditionalCase cond:
                    run = GetValue(machine, cond.Condition).IsOne;
                    break;
                case UnconditionalCase _:
                    run = true;
                    break;
                case OnceCase _:
                    run = firstUpdate;
                    break;
            }

            if (run)
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

                machine.SetOutput(setsingle.Output, value.IsOne);
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
                case NumberLiteralExpression num:
                    return new BitsValue(num.Value, num.Length);

                case ListExpression list:
                    return GetListValue(list);

                case OperatorExpression op:
                    return DoOperator(machine, op);

                case UnaryOperatorExpression unary:
                    return DoUnaryOperator(machine, unary);

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

                    if (value.IsOne)
                        n |= 1UL << (len - 1 - i);
                }

                return new BitsValue(items);
            }
        }

        private static BitsValue DoUnaryOperator(IMachine machine, UnaryOperatorExpression op)
        {
            var value = GetValue(machine, op.Operand);

            switch (op.Operator)
            {
                case Operator.Not:
                    return value.Negated;
                case Operator.And:
                    return value.AreAllBitsSet;
                case Operator.Or:
                    return value.IsAnyBitSet;
                case Operator.Truncate:
                    return value.Truncated;
            }

            throw new LogicEngineException();
        }

        private static BitsValue DoOperator(IMachine machine, OperatorExpression op)
        {
            var left = GetValue(machine, op.Left);
            var right = GetValue(machine, op.Right);

            switch (op.Operator)
            {
                case Operator.Add:
                    return left + right;
                case Operator.Subtract:
                    return left - right;
                case Operator.Multiply:
                    return left * right;
                case Operator.Divide:
                    return left / right;

                case Operator.Equals:
                    return left == right;
                case Operator.Greater:
                    return left > right;
                case Operator.GreaterOrEqual:
                    return left >= right;
                case Operator.Lesser:
                    return left < right;
                case Operator.LesserOrEqual:
                    return left <= right;

                case Operator.And:
                    return left & right;
                case Operator.Or:
                    return left | right;
            }

            throw new LogicEngineException();
        }
    }
}
