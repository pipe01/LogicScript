using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using LExpression = LogicScript.Parsing.Structures.Expressions.Expression;
using System.Linq.Expressions;
using Expression = System.Linq.Expressions.Expression;
using LogicScript.Parsing;

#if USE_FAST_EXPRESSIONS
using FastExpressionCompiler;
#endif

namespace LogicScript.Compiling
{
    public delegate void CompiledScript(IMachine machine, bool[] scratch, bool firstRun);

    public class Compiler
    {
        private class Scope(IDictionary<LocalInfo, ParameterExpression> locals)
        {
            public readonly IDictionary<LocalInfo, ParameterExpression> Locals = locals;
        }

        private readonly ParameterExpression Machine = Expression.Parameter(typeof(IMachine), "machine");
        private readonly ParameterExpression Scratch = Expression.Parameter(typeof(bool[]), "scratch");
        private readonly ParameterExpression FirstRun = Expression.Parameter(typeof(bool), "firstRun");

        private readonly Stack<Scope> Stack = new();
        private readonly Dictionary<NodeID, LabelTarget> LoopBreaks = [];

        private CompiledScript CompileScript(Script script)
        {
            if (script.HasErrors)
                throw new Exception("Script has errors");

            var body = new List<Expression>
            {
                Expression.IfThen(
                    FirstRun,
                    Expression.Call(
                        Machine,
                        typeof(IMachine).GetMethod(nameof(IMachine.AllocateRegisters)),
                        Expression.Constant(script.Registers.Sum(r => r.Value.VectorLength))
                    )
                )
            };
            body.AddRange(script.Blocks.Select(Compile));

            var ts = Expression.Lambda<CompiledScript>(Expression.Block(body), Machine, Scratch, FirstRun)
#if USE_FAST_EXPRESSIONS
            .CompileFast(flags: CompilerFlags.EnableDelegateDebugInfo);
#else
            .Compile();
#endif

#if USE_FAST_EXPRESSIONS
            TestTools.AllowPrintExpression = true;
            TestTools.AllowPrintCS = true;
            TestTools.AllowPrintIL = true;

            var d = ts.TryGetDebugInfo();
            d.PrintIL();
            // d.PrintCSharp();
#endif

            return ts;
        }

        public static CompiledScript Compile(Script script)
        {
            return new Compiler().CompileScript(script);
        }

        private Expression Compile(Block block)
        {
            return block switch
            {
                StartupBlock sb => Compile(sb),
                WhenBlock w => Compile(w),
                AssignBlock b => Compile(b.Assignment),
                _ => throw new NotImplementedException()
            };
        }

        private Expression Compile(StartupBlock block)
        {
            return Expression.IfThen(FirstRun, Compile(block.Body));
        }

        private Expression Compile(WhenBlock block)
        {
            var body = Compile(block.Body);

            var isConstantlyTrue = block.Condition?.IsConstant == true && GetConstantValue(block.Condition) != 0;

            return block.Condition == null || isConstantlyTrue ? body : Expression.IfThen(
                IsTruthy(Compile(block.Condition, true)),
                body
            );
        }

        private Expression Compile(Statement stmt)
        {
            return stmt switch
            {
                AssignStatement a => Compile(a),
                BlockStatement b => Compile(b),
                BreakStatement b => Compile(b),
                DeclareLocalStatement d => Compile(d),
                ForStatement f => Compile(f),
                IfStatement i => Compile(i),
                TaskStatement t => Compile(t),
                WhileStatement w => Compile(w),
                _ => throw new NotImplementedException()
            };
        }

        private Expression Compile(TaskStatement stmt)
        {
            switch (stmt)
            {
                case PrintTaskStatement print:
                    {
                        Expression text;

                        if (print.String.Interpolations.Count == 0)
                        {
                            text = Expression.Constant(print.String.Text);
                        }
                        else
                        {
                            var locals = print.String.Interpolations.Select(l => FindLocal(l.Local));
                            var fmtString = print.String.ToFormattable();

                            // TODO: optimize this to make less allocations
                            text = Expression.Call(
                                typeof(string).GetMethod(nameof(string.Format), [typeof(string), typeof(object[])]),
                                [
                                    Expression.Constant(fmtString),
                                    Expression.NewArrayInit(typeof(object), locals.Select(l => Expression.Convert(l, typeof(object))))
                                ]
                            );
                        }

                        return Expression.Call(
                            Machine,
                            typeof(IMachine).GetMethod(nameof(IMachine.Print)),
                            text
                        );
                    }

                case ShowTaskStatement show:
                    return Expression.Call(
                        Machine,
                        typeof(IMachine).GetMethod(nameof(IMachine.Print)),
                        Expression.Call(
                            Compile(show.Value, false),
                            typeof(object).GetMethod(nameof(object.ToString))
                        )
                    );

                case UpdateTaskStatement:
                    return Expression.Call(
                        Machine,
                        typeof(IMachine).GetMethod(nameof(IMachine.QueueUpdate))
                    );
            }

            throw new NotImplementedException();
        }

        private Expression Compile(BreakStatement stmt)
        {
            var label = LoopBreaks[stmt.TargetID]; // No need to check presence, the parser takes care of it

            return Expression.Break(label);
        }

        private Expression Compile(ForStatement stmt)
        {
            //TODO: optimize: compute 'to' once on loop enter and don't recompute on each iteration

            var from = stmt.From != null
                ? stmt.From.IsConstant
                    ? Expression.Constant(GetConstantValue(stmt.From))
                    : Compile(stmt.From, false)
                : Expression.Constant(0UL);
            var to = stmt.To.IsConstant ? Expression.Constant(GetConstantValue(stmt.To)) : Compile(stmt.To, false);
            var local = FindLocal(stmt.Variable);

            var breakLabel = Expression.Label("loop_break");

            LoopBreaks[stmt.ID] = breakLabel;
            var body = Compile(stmt.Body);
            LoopBreaks.Remove(stmt.ID);

            return Expression.Block(
                Expression.Assign(local, from),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.LessThan(local, to),
                        Expression.Block(
                            body,
                            Expression.PostIncrementAssign(local)
                        ),
                        Expression.Break(breakLabel)
                    ),
                    breakLabel
                )
            );
        }

        private Expression Compile(WhileStatement stmt)
        {
            var breakLabel = Expression.Label("loop_break");

            LoopBreaks[stmt.ID] = breakLabel;
            var body = Compile(stmt.Body);
            LoopBreaks.Remove(stmt.ID);

            return Expression.Loop(
                Expression.IfThenElse(
                    IsTruthy(Compile(stmt.Condition, true)),
                    body,
                    Expression.Break(breakLabel)
                ),
                breakLabel
            );
        }

        private Expression Compile(IfStatement stmt)
        {
            if (stmt.Condition.IsConstant)
            {
                var condConst = GetConstantValue(stmt.Condition);

                if (condConst != 0)
                    return Compile(stmt.Body);
                else if (stmt.Else != null)
                    return Compile(stmt.Else);
                else
                    return Expression.Empty();
            }

            var cond = IsTruthy(Compile(stmt.Condition, true));
            var then = Compile(stmt.Body);

            if (stmt.Else != null)
            {
                var @else = Compile(stmt.Else);

                return Expression.IfThenElse(cond, then, @else);
            }

            return Expression.IfThen(cond, then);
        }

        private Expression Compile(BlockStatement stmt)
        {
            var locals = stmt.Locals.ToDictionary(l => l, l => Expression.Variable(typeof(ulong), l.Name));

            Stack.Push(new(locals));

            var statements = stmt.Statements.Select(Compile).ToArray();

            Stack.Pop();

            return Expression.Block(locals.Values, statements);
        }

        private Expression Compile(AssignStatement stmt)
        {
            switch (stmt.Reference)
            {
                case PortReference port:
                    var startIndex = ComputePortOffset(port);

                    switch (port.PortInfo.Target)
                    {
                        case MachinePorts.Output:
                            var value = Compile(stmt.Value, port.BitSize == 1);

                            if (value.IsBool())
                            {
                                return Expression.Call(
                                    Machine,
                                    typeof(IMachine).GetMethod(nameof(IMachine.WriteOutput))!,
                                    startIndex,
                                    value
                                );
                            }
                            else
                            {
                                return Expression.Call(
                                    Machine,
                                    typeof(IMachine).GetMethod(nameof(IMachine.WriteOutputs)),
                                    startIndex,
                                    Expression.New(
                                        typeof(BitsValue).GetConstructor([typeof(ulong), typeof(int)])!,
                                        value,
                                        Expression.Constant(port.BitSize)
                                    )
                                );
                            }

                        case MachinePorts.Register:
                            return Expression.Call(
                                Machine,
                                typeof(IMachine).GetMethod(nameof(IMachine.WriteRegister)),
                                startIndex,
                                Compile(stmt.Value, false)
                            );
                    }
                    throw new NotImplementedException();

                case LocalReference local:
                    {
                        var localVar = FindLocal(local.LocalInfo);
                        var value = Compile(stmt.Value, false);

                        return Expression.Assign(localVar, value);
                    }
            }

            throw new NotImplementedException();
        }

        private Expression Compile(DeclareLocalStatement stmt)
        {
            if (stmt.Initializer is null)
                return Expression.Empty();

            var localVar = FindLocal(stmt.Local);

            return Expression.Assign(localVar, Compile(stmt.Initializer, false));
        }

        private Expression Compile(LExpression expr, bool canReturnBool)
        {
            return expr switch
            {
                BinaryOperatorExpression b => Compile(b, canReturnBool),
                NumberLiteralExpression n => canReturnBool ? Expression.Constant(n.Value != 0) : Expression.Constant(n.Value.Number),
                ReferenceExpression r => Compile(r, canReturnBool),
                SliceExpression s => Compile(s),
                TernaryOperatorExpression t => Compile(t, canReturnBool),
                TruncateExpression t => Compile(t.Operand, canReturnBool), // Truncating is a no-op at runtime since bit size is determined at compile-time
                UnaryOperatorExpression u => Compile(u, canReturnBool),
                ReferenceLengthExpression r => Expression.Constant((ulong)r.Value),
                _ => throw new NotImplementedException()
            };
        }

        private Expression Compile(TernaryOperatorExpression expr, bool canReturnBool)
        {
            if (expr.Condition.IsConstant)
            {
                var condConst = GetConstantValue(expr.Condition);

                if (condConst != 0)
                {
                    if (expr.IfTrue.IsConstant)
                        return Expression.Constant(GetConstantValue(expr.IfTrue));
                    else
                        return Compile(expr.IfTrue, canReturnBool);
                }
                else
                {
                    if (expr.IfFalse.IsConstant)
                        return Expression.Constant(GetConstantValue(expr.IfFalse));
                    else
                        return Compile(expr.IfFalse, canReturnBool);
                }
            }

            var cond = IsTruthy(Compile(expr.Condition, true));

            return Expression.Condition(
                cond,
                Compile(expr.IfTrue, canReturnBool),
                Compile(expr.IfFalse, canReturnBool)
            );
        }

        private Expression Compile(SliceExpression expr)
        {
            // TODO: check that this is right

            var operand = Compile(expr.Operand, false);

            var offset = expr.Start switch
            {
                IndexStart.Left => Compile(expr.Offset, false),
                IndexStart.Right => Expression.Subtract(
                    Expression.Constant((ulong)(expr.Operand.BitSize - expr.Length)),
                    Compile(expr.Offset, false)
                ),
                _ => throw new NotImplementedException(),
            };

            return Slice(operand, offset, expr.Length);
        }

        private Expression Compile(UnaryOperatorExpression expr, bool canReturnBool)
        {
            var inner = Compile(expr.Operand, canReturnBool && expr.Operator == Operator.Not);

            return expr.Operator switch
            {
                Operator.Not => inner.IsBool()
                    ? Expression.Not(inner)
                    : Negate(inner, expr.Operand.BitSize),
                Operator.Rise => throw new NotImplementedException(),
                Operator.Fall => throw new NotImplementedException(),
                Operator.Change => throw new NotImplementedException(),
                Operator.Length => Expression.Constant((ulong)expr.Operand.BitSize),
                Operator.AllOnes => BoolToNumberIf(AllOnes(inner, expr.Operand.BitSize), !canReturnBool),
                _ => throw new InterpreterException("Unknown operand", expr.Span),
            };
        }

        private Expression Compile(BinaryOperatorExpression expr, bool canReturnBool)
        {
            if (expr.Left.BitSize == 1 && expr.Right.BitSize == 1 && canReturnBool)
            {
                var leftBool = Compile(expr.Left, true);
                var rightBool = Compile(expr.Right, true);

                if (leftBool.IsBool() && rightBool.IsBool())
                {
                    switch (expr.Operator)
                    {
                        case Operator.And:
                            return Expression.AndAlso(leftBool, rightBool);
                        case Operator.Or:
                            return Expression.OrElse(leftBool, rightBool);
                        case Operator.EqualsCompare:
                            return Expression.Equal(leftBool, rightBool);
                        case Operator.NotEqualsCompare:
                        case Operator.Xor:
                            return Expression.NotEqual(leftBool, rightBool);
                    }
                }
            }

            var left = Compile(expr.Left, false);
            var right = Compile(expr.Right, false);

            return expr.Operator switch
            {
                Operator.And => Expression.And(left, right),
                Operator.Or => Expression.Or(left, right),
                Operator.Xor => Expression.ExclusiveOr(left, right),
                Operator.ShiftLeft => Expression.LeftShift(left, Expression.Convert(right, typeof(int))),
                Operator.ShiftRight => Expression.RightShift(left, Expression.Convert(right, typeof(int))),
                Operator.Add => Expression.Add(left, right),
                Operator.Subtract => Expression.Subtract(left, right),
                Operator.Multiply => Expression.Multiply(left, right),
                Operator.Divide => Expression.Divide(left, right),
                Operator.Power => Expression.Convert(
                    Expression.Power(
                        Expression.Convert(left, typeof(double)),
                        Expression.Convert(right, typeof(double))
                    ),
                    typeof(ulong)
                ),
                Operator.Modulus => Expression.Modulo(left, right),
                Operator.EqualsCompare => BoolToNumberIf(Expression.Equal(left, right), !canReturnBool),
                Operator.NotEqualsCompare => BoolToNumberIf(Expression.NotEqual(left, right), !canReturnBool),
                Operator.Greater => BoolToNumberIf(Expression.GreaterThan(left, right), !canReturnBool),
                Operator.Lesser => BoolToNumberIf(Expression.LessThan(left, right), !canReturnBool),
                _ => throw new InterpreterException("Unknown operator", expr.Span)
            };
        }

        private Expression Compile(ReferenceExpression expr, bool canReturnBool)
        {
            switch (expr.Reference)
            {
                case LocalReference local:
                    return FindLocal(local.LocalInfo);

                case PortReference port:
                    var startIndex = ComputePortOffset(port);

                    return port.PortInfo.Target switch
                    {
                        MachinePorts.Input => expr.BitSize == 1 && canReturnBool
                            ? Expression.Call(
                                Machine,
                                typeof(IMachine).GetMethod(nameof(IMachine.ReadInput)),
                                startIndex
                            )
                            : ReadInput(startIndex, port.BitSize),
                        MachinePorts.Register => Expression.Call(
                            Machine,
                            typeof(IMachine).GetMethod(nameof(IMachine.ReadRegister)),
                            startIndex
                        ),
                        _ => throw new NotImplementedException()
                    };

                default:
                    throw new NotImplementedException();
            }
        }

        private ParameterExpression FindLocal(LocalInfo info)
        {
            foreach (var scope in Stack)
            {
                if (scope.Locals.TryGetValue(info, out var local))
                    return local;
            }

            throw new Exception($"Local {info} not found");
        }

        private static Expression IsTruthy(Expression value)
        {
            if (value.IsBool()) return value;

            return Expression.NotEqual(value, Expression.Constant(0UL));
        }

        private ulong GetConstantValue(LExpression expr) => GetConstantValue(Compile(expr, false));
        private static ulong GetConstantValue(Expression expr)
        {
            return Expression.Lambda<Func<ulong>>(expr)
#if USE_FAST_EXPRESSIONS
                .TryCompile<Func<ulong>>()();
#else
                .Compile()();
#endif
        }

        private static Expression Slice(Expression value, Expression start, int length)
        {
            return Expression.And(
                Expression.RightShift(value, Expression.Convert(start, typeof(int))),
                Expression.Constant((1UL << length) - 1)
            );
        }

        private static Expression AllOnes(Expression value, int length)
        {
            return Expression.Equal(
                value,
                Expression.Constant((1UL << length) - 1)
            );
        }

        private static Expression Negate(Expression value, int length)
        {
            return Expression.And(
                Expression.OnesComplement(value),
                Expression.Constant((1UL << length) - 1)
            );
        }

        private static Expression BoolToNumberIf(Expression boolExpr, bool convert)
        {
            return convert ? Expression.Condition(boolExpr, Expression.Constant(1UL), Expression.Constant(0UL)) : boolExpr;
        }

        private Expression ReadInput(Expression start, int size)
        {
            return Expression.Field(
                Expression.Call(
                    Expression.Call(
                        Machine,
                        typeof(IMachine).GetMethod(nameof(IMachine.ReadInputs))
                    ),
                    typeof(BitsValue).GetMethod(nameof(BitsValue.Slice)),
                    start,
                    Expression.Constant(size)
                ),
                typeof(BitsValue).GetField(nameof(BitsValue.Number))
            );
        }


        private Expression ComputePortOffset(PortReference port)
        {
            if (port.VectorIndex == null)
            {
                return Expression.Constant(port.StartIndex);
            }
            else
            {
                if (port.VectorIndex.IsConstant)
                {
                    var vectorIndex = (int)GetConstantValue(port.VectorIndex);
                    return port.PortInfo.Target == MachinePorts.Register
                        ? Expression.Constant(port.StartIndex + vectorIndex)
                        : Expression.Constant(port.StartIndex + port.BitSize * vectorIndex);
                }
                else
                {
                    return Expression.Add(
                        Expression.Constant(port.StartIndex),
                        port.PortInfo.Target == MachinePorts.Register
                            ? Compile(port.VectorIndex, false)
                            : Expression.Multiply(
                                Expression.Constant(port.BitSize),
                                Compile(port.VectorIndex, false)
                            )
                    );
                }
            }
        }
    }
}
