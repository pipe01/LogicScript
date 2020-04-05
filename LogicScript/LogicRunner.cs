using LogicScript.Data;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;

namespace LogicScript
{
    public static class LogicRunner
    {
        public static void RunScript(Script script, IMachine machine, bool isFirstUpdate = false)
        {
            int len = script.Cases.Count;
            for (int i = 0; i < len; i++)
            {
                UpdateCase(machine, script.Cases[i], isFirstUpdate);
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
            if (stmt is AssignStatement assign)
            {
                var value = GetValue(machine, assign.Value);

                if (assign.Slot.IsIndexed)
                {
                    if (!value.IsSingleBit)
                        throw new LogicEngineException("Expected single-bit value", assign.Value);

                    int index = assign.Slot.Index.Value;

                    switch (assign.Slot.Slot)
                    {
                        case Slots.Out:
                            machine.SetOutput(index, value.IsOne);
                            break;
                        case Slots.Memory:
                            machine.Memory.SetBit(index, value.IsOne);
                            break;
                    }
                }
                else
                {
                    switch (assign.Slot.Slot)
                    {
                        case Slots.Out:
                            machine.SetOutputs(value);
                            break;
                        case Slots.Memory:
                            machine.Memory.Set(value);
                            break;
                    }
                }
            }
        }

        private static BitsValue GetValue(IMachine machine, Expression expr)
        {
            switch (expr)
            {
                case NumberLiteralExpression num:
                    return new BitsValue(num.Value, num.Length);

                case ListExpression list:
                    return DoListExpression(machine, list);

                case OperatorExpression op:
                    return DoOperator(machine, op);

                case UnaryOperatorExpression unary:
                    return DoUnaryOperator(machine, unary);

                case SlotExpression slot:
                    return DoSlotExpression(machine, slot);

                default:
                    throw new LogicEngineException("Expected multi-bit value", expr);
            }
        }

        private static BitsValue DoSlotExpression(IMachine machine, SlotExpression expr)
        {
            switch (expr.Slot)
            {
                case Slots.In:
                    return expr.IsIndexed ? machine.GetInput(expr.Index.Value) : machine.GetInputs();
                case Slots.Memory:
                    return expr.IsIndexed ? machine.Memory.GetBit(expr.Index.Value) : machine.Memory.Get();
            }

            throw new LogicEngineException("Invalid slot on expression");
        }

        private static BitsValue DoListExpression(IMachine machine, ListExpression list)
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
