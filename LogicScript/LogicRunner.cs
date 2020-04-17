using LogicScript.Data;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace LogicScript
{
    public static class LogicRunner
    {
        public static void RunScript(Script script, IMachine machine, bool isFirstUpdate = false)
        {
            int len = script.TopLevelNodes.Count;
            for (int i = 0; i < len; i++)
            {
                var node = script.TopLevelNodes[i];

                if (node is Case @case)
                    UpdateCase(machine, @case, isFirstUpdate);
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
                    run = IsTruthy(GetValue(machine, cond.Condition));
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

        private static void RunStatements(IMachine machine, IReadOnlyList<Statement> statements)
        {
            for (int i = 0; i < statements.Count; i++)
            {
                RunStatement(machine, statements[i]);
            }
        }

        private static void RunStatement(IMachine machine, Statement stmt)
        {
            //Console.WriteLine("> " + stmt);

            switch (stmt)
            {
                case AssignStatement assign:
                    RunStatement(machine, assign);
                    break;
                case IfStatement @if:
                    RunStatement(machine, @if);
                    break;
                case QueueUpdateStatement queueStmt:
                    RunStatement(machine, queueStmt);
                    break;
            }
        }

        private static bool IsTruthy(BitsValue value) => value.Number > 0;

        private static void RunStatement(IMachine machine, AssignStatement stmt)
        {
            if (!stmt.LeftSide.IsWriteable)
                throw new LogicEngineException("Expected a writeable expression", stmt);
            
            if (!stmt.RightSide.IsReadable)
                throw new LogicEngineException("Expected a readable expression", stmt);

            var lhs = stmt.LeftSide;

            var value = GetValue(machine, stmt.RightSide);
            BitRange range;

            if (lhs is IndexerExpression indexer)
            {
                range = indexer.Range;
                lhs = indexer.Operand;
            }
            else
            {
                range = new BitRange(0, value.Length);
            }

            if (!range.HasEnd)
                range = new BitRange(range.Start, range.Start + value.Length);

            if (value.Length != range.Length)
                throw new LogicEngineException("Range and value length mismatch", stmt);

            Span<bool> bits = stackalloc bool[value.Length];
            value.FillBits(bits);

            if (lhs is SlotExpression slot)
            {
                switch (slot.Slot)
                {
                    case Slots.Out:
                        if (range.Start + range.Length > machine.OutputCount)
                            throw new LogicEngineException("Range out of bounds for outputs", stmt);

                        machine.SetOutputs(range, bits);
                        break;

                    case Slots.Memory:
                        if (range.Start + range.Length > machine.Memory.Capacity)
                            throw new LogicEngineException("Range out of bounds for memory", stmt);

                        machine.Memory.Write(range, bits);
                        break;

                    default:
                        throw new LogicEngineException("Invalid slot on expression", stmt);
                }
            }
        }

        private static void RunStatement(IMachine machine, IfStatement stmt)
        {
            var conditionValue = GetValue(machine, stmt.Condition);

            if (IsTruthy(conditionValue))
            {
                RunStatements(machine, stmt.Body);
            }
            else if (stmt.Else != null)
            {
                RunStatements(machine, stmt.Else);
            }
        }

        private static void RunStatement(IMachine machine, QueueUpdateStatement stmt)
        {
            if (!(machine is IUpdatableMachine updatableMachine))
                throw new LogicEngineException("Update queueing is not supported by the machine", stmt);

            updatableMachine.QueueUpdate();
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

                case IndexerExpression indexer when indexer.Operand is SlotExpression slot:
                    return DoSlotExpression(machine, slot, indexer.Range);

                case SlotExpression slot:
                    return DoSlotExpression(machine, slot, null);

                case FunctionCallExpression funcCall:
                    return DoFunctionCall(machine, funcCall);

                default:
                    throw new LogicEngineException("Expected multi-bit value", expr);
            }
        }

        private static BitsValue DoFunctionCall(IMachine machine, FunctionCallExpression funcCall)
        {
            Span<BitsValue> values = stackalloc BitsValue[funcCall.Arguments.Count];

            for (int i = 0; i < funcCall.Arguments.Count; i++)
            {
                values[i] = GetValue(machine, funcCall.Arguments[i]);
            }

            switch (funcCall.Name)
            {
                case "and":
                    if (values.Length != 1)
                        throw new LogicEngineException("Expected 1 argument on call to 'add'", funcCall);
                    return values[0].AreAllBitsSet;

                case "or":
                    if (values.Length != 1)
                        throw new LogicEngineException("Expected 1 argument on call to 'or'", funcCall);
                    return values[0].IsAnyBitSet;

                case "sum":
                    if (values.Length != 1)
                        throw new LogicEngineException("Expected 1 argument on call to 'sum'", funcCall);
                    return values[0].PopulationCount;

                case "trunc" when values.Length == 2:
                    return new BitsValue(values[0], (int)values[1].Number);

                case "trunc" when values.Length == 1:
                    return values[0].Truncated;

                case "trunc":
                    throw new LogicEngineException("Expected 1 or 2 arguments", funcCall);

                default:
                    throw new LogicEngineException($"Unknown function '{funcCall.Name}'", funcCall);
            }
        }

        private static BitsValue DoSlotExpression(IMachine machine, SlotExpression expr, BitRange? r)
        {
            var range = r ?? new BitRange(0, machine.InputCount);
            if (!range.HasEnd)
                range = new BitRange(range.Start, machine.InputCount);

            Span<bool> values = stackalloc bool[range.Length];

            switch (expr.Slot)
            {
                case Slots.In:
                    machine.GetInputs(range, values);
                    break;
                case Slots.Memory:
                    machine.Memory.Read(range, values);
                    break;
                default:
                    throw new LogicEngineException("Invalid slot on expression", expr);
            }

            return new BitsValue(values);
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
                case Operator.Modulo:
                    return left.Number % right.Number;

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
                    return new BitsValue(left & right, Math.Min(left.Length, right.Length));
                case Operator.Or:
                    return new BitsValue(left | right, Math.Min(left.Length, right.Length));
            }

            throw new LogicEngineException();
        }
    }
}
