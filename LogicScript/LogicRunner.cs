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
        internal struct CaseContext
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
                {
                    var ctx = new CaseContext(machine, script);
                    UpdateCase(ref ctx, @case, isFirstUpdate);
                }
            }
        }

        private static void UpdateCase(ref CaseContext ctx, Case c, bool firstUpdate)
        {
            if (c.Statements == null)
                return;

            bool run = false;

            switch (c)
            {
                case ConditionalCase cond:
                    run = IsTruthy(GetValue(ref ctx, cond.Condition));
                    break;
                case UnconditionalCase _:
                    run = true;
                    break;
                case OnceCase _:
                    run = firstUpdate;
                    break;
            }

            if (run)
                RunStatements(ref ctx, c.Statements);
        }

        private static void RunStatements(ref CaseContext ctx, IReadOnlyList<Statement> statements)
        {
            for (int i = 0; i < statements.Count; i++)
            {
                RunStatement(ref ctx, statements[i]);
            }
        }

        private static void RunStatement(ref CaseContext ctx, Statement stmt)
        {
            switch (stmt)
            {
                case ExpressionStatement exprStmt:
                    GetValue(ref ctx, exprStmt.Expression);
                    break;
                case IfStatement @if:
                    RunStatement(ref ctx, @if);
                    break;
                case QueueUpdateStatement queueStmt:
                    RunStatement(ref ctx, queueStmt);
                    break;
                case ForStatement forStatement:
                    RunStatement(ref ctx, forStatement);
                    break;
            }
        }

        private static bool IsTruthy(BitsValue value) => value.Number > 0;

        private static void RunStatement(ref CaseContext ctx, IfStatement stmt)
        {
            var conditionValue = GetValue(ref ctx, stmt.Condition);

            if (IsTruthy(conditionValue))
            {
                RunStatements(ref ctx, stmt.Body);
            }
            else if (stmt.Else != null)
            {
                RunStatements(ref ctx, stmt.Else);
            }
        }

        private static void RunStatement(ref CaseContext ctx, ForStatement stmt)
        {
            var from = GetValue(ref ctx, stmt.From);
            var to = GetValue(ref ctx, stmt.To);

            for (ulong i = from.Number; i < to.Number; i++)
            {
                ctx.Set(stmt.VarName, new BitsValue(i, to.Length));
                RunStatements(ref ctx, stmt.Body);
            }
        }

        private static void RunStatement(ref CaseContext ctx, QueueUpdateStatement stmt)
        {
            if (!(ctx.Machine is IUpdatableMachine updatableMachine))
                throw new LogicEngineException("Update queueing is not supported by the machine", stmt);

            updatableMachine.QueueUpdate();
        }

        internal static BitsValue GetValue(ref CaseContext ctx, Expression expr)
        {
            switch (expr.Type)
            {
                case ExpressionType.FunctionCall:
                    return DoFunctionCall(ref ctx, (FunctionCallExpression)expr);

                case ExpressionType.Indexer:
                    var indexer = (IndexerExpression)expr;
                    if (indexer.Operand.Type == ExpressionType.Slot)
                        return DoSlotExpression(ref ctx, (SlotExpression)indexer.Operand, indexer);
                    return DoIndexerExpression(ref ctx, (IndexerExpression)expr);

                case ExpressionType.List:
                    return DoListExpression(ref ctx, (ListExpression)expr);

                case ExpressionType.NumberLiteral:
                    return ((NumberLiteralExpression)expr).Value;

                case ExpressionType.Operator:
                    return DoOperator(ref ctx, (OperatorExpression)expr);

                case ExpressionType.Slot:
                    return DoSlotExpression(ref ctx, (SlotExpression)expr, null);

                case ExpressionType.UnaryOperator:
                    return DoUnaryOperator(ref ctx, (UnaryOperatorExpression)expr);

                case ExpressionType.VariableAccess:
                    return ctx.Get(((VariableAccessExpression)expr).Name, expr);
            }

            throw new LogicEngineException("Expected multi-bit value", expr);
        }

        private static void GetRange(ref CaseContext ctx, IndexerExpression indexer, int length, out int start, out int end)
        {
            start = (int)GetValue(ref ctx, indexer.Start).Number;

            end = indexer.HasEnd
                ? indexer.End == null
                    ? start + 1
                    : (int)GetValue(ref ctx, indexer.End).Number
                : length;
        }

        private static BitsValue DoIndexerExpression(ref CaseContext ctx, IndexerExpression indexer)
        {
            var value = GetValue(ref ctx, indexer.Operand);
            GetRange(ref ctx, indexer, value.Length, out var start, out var end);

            Span<bool> bits = stackalloc bool[end - start];
            value.FillBits(bits, start, end);

            return new BitsValue(bits);
        }

        private static BitsValue DoFunctionCall(ref CaseContext ctx, FunctionCallExpression funcCall)
        {
            Span<BitsValue> values = stackalloc BitsValue[funcCall.Arguments.Count];

            for (int i = 0; i < funcCall.Arguments.Count; i++)
            {
                values[i] = GetValue(ref ctx, funcCall.Arguments[i]);
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

        private static BitsValue DoSlotExpression(ref CaseContext ctx, SlotExpression expr, IndexerExpression indexer)
        {
            int maxLength =
                expr.Slot == Slots.In ? ctx.Machine.InputCount :
                expr.Slot == Slots.Memory ? ctx.Machine.Memory.Capacity : throw new LogicEngineException("Invalid slot", expr);

            GetRange(ref ctx, indexer, maxLength, out var start, out var end);
            Span<bool> values = stackalloc bool[end - start];

            switch (expr.Slot)
            {
                case Slots.In:
                    //ctx.Machine.GetInputs(start, values);
                    break;
                case Slots.Memory:
                    //ctx.Machine.Memory.Read(start, new BitsValue(values));
                    break;
                default:
                    throw new LogicEngineException("Invalid slot on expression", expr);
            }

            return new BitsValue(values);
        }

        private static BitsValue DoListExpression(ref CaseContext ctx, ListExpression list)
        {
            ulong n = 0;

            int len = list.Expressions.Length;
            for (int i = 0; i < len; i++)
            {
                var value = GetValue(ref ctx, list.Expressions[i]);

                if (!value.IsSingleBit)
                    throw new LogicEngineException("List expressions can only contain single-bit values", list.Expressions[i]);

                if (value.IsOne)
                    n |= 1UL << (len - 1 - i);
            }

            return new BitsValue(n, len);
        }

        private static BitsValue DoUnaryOperator(ref CaseContext ctx, UnaryOperatorExpression op)
        {
            var value = GetValue(ref ctx, op.Operand);

            switch (op.Operator)
            {
                case Operator.Not:
                    return value.Negated;
            }

            throw new LogicEngineException();
        }

        private static BitsValue DoOperator(ref CaseContext ctx, OperatorExpression op)
        {
            if (op.Operator == Operator.Assign)
                return DoAssignment(ref ctx, op);

            var left = GetValue(ref ctx, op.Left);
            var right = GetValue(ref ctx, op.Right);

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
                case Operator.NotEquals:
                    return left != right;
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

        private static BitsValue DoAssignment(ref CaseContext ctx, OperatorExpression op)
        {
            if (!op.Left.IsWriteable)
                throw new LogicEngineException("Expected a writeable expression", op);

            if (!op.Right.IsReadable)
                throw new LogicEngineException("Expected a readable expression", op);

            var lhs = op.Left;

            var value = GetValue(ref ctx, op.Right);
            int start, end;
            bool isRanged = false;

            if (lhs is IndexerExpression indexer)
            {
                GetRange(ref ctx, indexer, value.Length, out start, out end);
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

                        ctx.Machine.SetOutputs(start, value);
                        break;

                    case Slots.Memory:
                        if (end > ctx.Machine.Memory.Capacity)
                            throw new LogicEngineException("Range out of bounds for memory", op);

                        //ctx.Machine.Memory.Write(start, bits);
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
