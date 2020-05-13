using LogicScript.Data;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LogicScript
{
    public static class LogicRunner
    {
        internal ref struct CaseContext
        {
            public readonly IMachine Machine;
            public readonly IDictionary<string, BitsValue> Variables;
            public readonly Script Script;

            public CaseContext(IMachine machine, Script script)
            {
                this.Machine = machine;
                this.Script = script;
                this.Variables = new Dictionary<string, BitsValue>();
            }

            public void Set(string name, BitsValue val) => Variables[name] = val;

            public void Unset(string name) => Variables.Remove(name);

            public bool Has(string name) => Variables.ContainsKey(name);

            public BitsValue Get(string name, ICodeNode node)
            {
                if (!Variables.TryGetValue(name, out var val))
                {
                    if (Script.Strict)
                        throw new LogicEngineException($"Variable \"{name}\" not defined", node);
                    else
                        return BitsValue.Zero;
                }

                return val;
            }
        }

        public static void RunScript(Script script, IMachine machine, bool isFirstUpdate = false)
        {
            int len = script.TopLevelNodes.Count;
            for (int i = 0; i < len; i++)
            {
                var node = script.TopLevelNodes[i];

                if (node is Case @case)
                    UpdateCase(new CaseContext(machine, script), @case, isFirstUpdate);
            }
        }

        private static void UpdateCase(CaseContext ctx, Case c, bool firstUpdate)
        {
            if (c.Statements == null)
                return;

            bool run = false;

            switch (c)
            {
                case ConditionalCase cond:
                    run = IsTruthy(GetValue(ctx, cond.Condition));
                    break;
                case UnconditionalCase _:
                    run = true;
                    break;
                case OnceCase _:
                    run = firstUpdate;
                    break;
            }

            if (run)
                RunStatements(ctx, c.Statements);
        }

        private static void RunStatements(CaseContext ctx, IReadOnlyList<Statement> statements)
        {
            for (int i = 0; i < statements.Count; i++)
            {
                RunStatement(ctx, statements[i]);
            }
        }

        private static void RunStatement(CaseContext ctx, Statement stmt)
        {
            switch (stmt)
            {
                case ExpressionStatement exprStmt:
                    GetValue(ctx, exprStmt.Expression);
                    break;
                case IfStatement @if:
                    RunStatement(ctx, @if);
                    break;
                case QueueUpdateStatement queueStmt:
                    RunStatement(ctx, queueStmt);
                    break;
                case ForStatement forStatement:
                    RunStatement(ctx, forStatement);
                    break;
            }
        }

        private static bool IsTruthy(BitsValue value) => value.Number > 0;

        private static void RunStatement(CaseContext ctx, IfStatement stmt)
        {
            var conditionValue = GetValue(ctx, stmt.Condition);

            if (IsTruthy(conditionValue))
            {
                RunStatements(ctx, stmt.Body);
            }
            else if (stmt.Else != null)
            {
                RunStatements(ctx, stmt.Else);
            }
        }

        private static void RunStatement(CaseContext ctx, ForStatement stmt)
        {
            var from = GetValue(ctx, stmt.From);
            var to = GetValue(ctx, stmt.To);

            for (ulong i = from.Number; i < to.Number; i++)
            {
                ctx.Set(stmt.VarName, new BitsValue(i, to.Length));
                RunStatements(ctx, stmt.Body);
            }
        }

        private static void RunStatement(CaseContext ctx, QueueUpdateStatement stmt)
        {
            if (!(ctx.Machine is IUpdatableMachine updatableMachine))
                throw new LogicEngineException("Update queueing is not supported by the machine", stmt);

            updatableMachine.QueueUpdate();
        }

        internal static BitsValue GetValue(CaseContext ctx, Expression expr)
        {
            switch (expr.Type)
            {
                case ExpressionType.FunctionCall:
                    return DoFunctionCall(ctx, (FunctionCallExpression)expr);

                case ExpressionType.Indexer:
                    var indexer = (IndexerExpression)expr;
                    if (indexer.Operand.Type == ExpressionType.Slot)
                        return DoSlotExpression(ctx, (SlotExpression)indexer.Operand, indexer);
                    return DoIndexerExpression(ctx, (IndexerExpression)expr);

                case ExpressionType.List:
                    return DoListExpression(ctx, (ListExpression)expr);

                case ExpressionType.NumberLiteral:
                    return ((NumberLiteralExpression)expr).Value;

                case ExpressionType.Operator:
                    return DoOperator(ctx, (OperatorExpression)expr);

                case ExpressionType.Slot:
                    return DoSlotExpression(ctx, (SlotExpression)expr, null);

                case ExpressionType.UnaryOperator:
                    return DoUnaryOperator(ctx, (UnaryOperatorExpression)expr);

                case ExpressionType.VariableAccess:
                    return ctx.Get(((VariableAccessExpression)expr).Name, expr);
            }

            throw new LogicEngineException("Expected multi-bit value", expr);
        }

        private static void GetRange(CaseContext ctx, IndexerExpression indexer, int length, out int start, out int end)
        {
            start = (int)GetValue(ctx, indexer.Start).Number;

            end = indexer.HasEnd
                ? indexer.End == null ? start + 1
                : (int)GetValue(ctx, indexer.End).Number : length;
        }

        private static BitsValue DoIndexerExpression(CaseContext ctx, IndexerExpression indexer)
        {
            var value = GetValue(ctx, indexer.Operand);
            GetRange(ctx, indexer, value.Length, out var start, out var end);

            Span<bool> bits = stackalloc bool[end - start];
            value.FillBits(bits, start, end);

            return new BitsValue(bits);
        }

        private static BitsValue DoFunctionCall(CaseContext ctx, FunctionCallExpression funcCall)
        {
            Span<BitsValue> values = stackalloc BitsValue[funcCall.Arguments.Count];

            for (int i = 0; i < funcCall.Arguments.Count; i++)
            {
                values[i] = GetValue(ctx, funcCall.Arguments[i]);
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
                    throw new LogicEngineException($"Expected 1 or 2 arguments on call to 'trunc', got {values.Length}", funcCall);

                default:
                    throw new LogicEngineException($"Unknown function '{funcCall.Name}'", funcCall);
            }
        }

        private static BitsValue DoSlotExpression(CaseContext ctx, SlotExpression expr, IndexerExpression indexer)
        {
            int maxLength =
                expr.Slot == Slots.In ? ctx.Machine.InputCount :
                expr.Slot == Slots.Memory ? ctx.Machine.Memory.Capacity : throw new LogicEngineException("Invalid slot", expr);

            GetRange(ctx, indexer, maxLength, out var start, out var end);
            Span<bool> values = stackalloc bool[end - start];

            switch (expr.Slot)
            {
                case Slots.In:
                    ctx.Machine.GetInputs(start, values);
                    break;
                case Slots.Memory:
                    ctx.Machine.Memory.Read(start, values);
                    break;
                default:
                    throw new LogicEngineException("Invalid slot on expression", expr);
            }

            return new BitsValue(values);
        }

        private static BitsValue DoListExpression(CaseContext ctx, ListExpression list)
        {
            ulong n = 0;

            int len = list.Expressions.Length;
            for (int i = 0; i < len; i++)
            {
                var value = GetValue(ctx, list.Expressions[i]);

                if (!value.IsSingleBit)
                    throw new LogicEngineException("List expressions can only contain single-bit values", list.Expressions[i]);

                if (value.IsOne)
                    n |= 1UL << (len - 1 - i);
            }

            return new BitsValue(n, len);
        }

        private static BitsValue DoUnaryOperator(CaseContext ctx, UnaryOperatorExpression op)
        {
            var value = GetValue(ctx, op.Operand);

            switch (op.Operator)
            {
                case Operator.Not:
                    return value.Negated;
            }

            throw new LogicEngineException();
        }

        private static BitsValue DoOperator(CaseContext ctx, OperatorExpression op)
        {
            if (op.Operator == Operator.Assign)
                return DoAssignment(ctx, op);

            var left = GetValue(ctx, op.Left);
            var right = GetValue(ctx, op.Right);

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
                    return new BitsValue(left | right, Math.Max(left.Length, right.Length));

                case Operator.Xor:
                    return new BitsValue(left ^ right, Math.Max(left.Length, right.Length));
            }

            throw new LogicEngineException();
        }

        private static BitsValue DoAssignment(CaseContext ctx, OperatorExpression op)
        {
            if (!op.Left.IsWriteable)
                throw new LogicEngineException("Expected a writeable expression", op);

            if (!op.Right.IsReadable)
                throw new LogicEngineException("Expected a readable expression", op);

            var lhs = op.Left;

            var value = GetValue(ctx, op.Right);
            int start, end;
            bool isRanged = false;

            if (lhs is IndexerExpression indexer)
            {
                GetRange(ctx, indexer, value.Length, out start, out end);
                lhs = indexer.Operand;
                isRanged = true;
            }
            else
            {
                start = 0;
                end = GetLength(ctx.Machine, lhs);
            }

            if (end == 0)
                end = start + value.Length;

            if (value.Length > end - start)
                throw new LogicEngineException("Value doesn't fit in range", op);

            Span<bool> bits = stackalloc bool[value.Length];
            value.FillBits(bits);

            if (lhs is SlotExpression slot)
            {
                switch (slot.Slot)
                {
                    case Slots.Out:
                        if (end > ctx.Machine.OutputCount)
                            throw new LogicEngineException("Range out of bounds for outputs", op);

                        ctx.Machine.SetOutputs(start, bits);
                        break;

                    case Slots.Memory:
                        if (end > ctx.Machine.Memory.Capacity)
                            throw new LogicEngineException("Range out of bounds for memory", op);

                        ctx.Machine.Memory.Write(start, bits);
                        break;

                    default:
                        throw new LogicEngineException("Invalid slot on expression", op);
                }
            }
            else if (lhs is VariableAccessExpression var)
            {
                if (!isRanged)
                {
                    ctx.Set(var.Name, value);
                }
                else
                {
                    //var val = ctx.Get(var.Name, var);
                    throw new LogicEngineException("Cannot index variable", var);
                }
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetLength(IMachine machine, Expression expr)
        {
            if (expr is SlotExpression s)
            {
                if (s.Slot == Slots.Out)
                    return machine.OutputCount;
                else if (s.Slot == Slots.Memory)
                    return machine.Memory.Capacity;
            }
            else if (expr.Type == ExpressionType.VariableAccess)
            {
                return BitsValue.BitSize;
            }

            return 0;
        }
    }
}
