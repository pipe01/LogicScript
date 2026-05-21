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

#if USE_FAST_EXPRESSIONS
using FastExpressionCompiler.LightExpression;
using Expression = FastExpressionCompiler.LightExpression.Expression;
#else
using System.Linq.Expressions;
using Expression = System.Linq.Expressions.Expression;
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

        private Compiler()
        {
        }

        private CompiledScript CompileInner(Script script)
        {
            var expr = Expression.Block(script.Blocks.Select(Compile));

            var ts = Expression.Lambda<CompiledScript>(expr, Machine, Scratch, FirstRun)
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
            return new Compiler().CompileInner(script);
        }

        private Expression Compile(Block block)
        {
            return block switch
            {
                StartupBlock sb => Compile(sb),
                WhenBlock w => Compile(w),
                _ => throw new NotImplementedException()
            };
        }

        private Expression Compile(StartupBlock block)
        {
            return Compile(block.Body);
        }

        private Expression Compile(WhenBlock block)
        {
            var body = Compile(block.Body);

            var isConstantlyTrue = block.Condition?.IsConstant == true && GetConstantValue(block.Condition).Number != 0;

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
                DeclareLocalStatement d => Compile(d),
                BlockStatement b => Compile(b),
                _ => throw new NotImplementedException()
            };
        }

        private Expression Compile(BlockStatement stmt)
        {
            var locals = stmt.Locals.Select(l => (l.Value, Expression.Variable(typeof(ulong), l.Value.OriginalName))).ToDictionary(l => l.Value, l => l.Item2);

            Stack.Push(new(locals));

            var statements = stmt.Statements.Select(Compile).ToArray();

            Stack.Pop();

            return Expression.Block(locals.Values, statements);
        }

        private Expression Compile(AssignStatement stmt)
        {
            switch (stmt.Reference.Port)
            {
                case PortInfo port:
                    if (port.Target == MachinePorts.Output)
                    {
                        var value = Compile(stmt.Value, port.BitSize == 1);

                        if (value.IsBool())
                        {
                            return Expression.Call(
                                Machine,
                                typeof(IMachine).GetMethod(nameof(IMachine.WriteOutput))!,
                                Expression.Constant(port.StartIndex),
                                value
                            );
                        }
                        else
                        {
                            return Expression.Call(
                                Machine,
                                typeof(IMachine).GetMethod(nameof(IMachine.WriteOutputs)),
                                Expression.Constant(port.StartIndex),
                                Expression.New(
                                    typeof(BitsValue).GetConstructor([typeof(ulong), typeof(int)])!,
                                    value,
                                    Expression.Constant(port.BitSize)
                                )
                            );
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                case LocalInfo local:
                    {
                        var localVar = FindLocal(local) ?? throw new Exception("local not found");
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

            var localVar = FindLocal(stmt.Local) ?? throw new Exception("local not found");

            return Expression.Assign(localVar, Compile(stmt.Initializer, false));
        }

        private Expression Compile(LExpression expr, bool canReturnBool)
        {
            return expr switch
            {
                BinaryOperatorExpression b => Compile(b, canReturnBool),
                UnaryOperatorExpression u => Compile(u, canReturnBool),
                NumberLiteralExpression n => canReturnBool ? Expression.Constant(n.Value != 0) : Expression.Constant(n.Value.Number),
                ReferenceExpression r => Compile(r, canReturnBool),
                TruncateExpression t => Compile(t),
                _ => throw new NotImplementedException()
            };
        }

        private Expression Compile(TruncateExpression expr)
        {
            var inner = Compile(expr.Operand, false);

            if (expr.Operand.BitSize <= expr.Size)
                return inner;

            return Expression.And(inner, Expression.Constant((1UL << expr.Size) - 1));
        }

        private Expression Compile(UnaryOperatorExpression expr, bool canReturnBool)
        {
            var inner = Compile(expr.Operand, canReturnBool && expr.Operator == Operator.Not);

            return expr.Operator switch
            {
                Operator.Not => inner.IsBool()
                    ? Expression.Not(inner)
                    : Expression.OnesComplement(inner),
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
            return expr.Reference.Port switch
            {
                PortInfo port => port.Target switch
                {
                    MachinePorts.Input => expr.BitSize == 1 && canReturnBool
                        ? Expression.Call(
                            Machine,
                            typeof(IMachine).GetMethod(nameof(IMachine.ReadInput)),
                            Expression.Constant(port.StartIndex)
                        )
                        : ReadInput(port.StartIndex, port.BitSize),
                    _ => throw new NotImplementedException()
                },
                LocalInfo local => FindLocal(local) ?? throw new Exception("local not found"),
                _ => throw new NotImplementedException()
            };
        }

        private ParameterExpression? FindLocal(LocalInfo info)
        {
            foreach (var scope in Stack)
            {
                if (scope.Locals.TryGetValue(info, out var local))
                    return local;
            }

            return null;
        }

        private static Expression IsTruthy(Expression value)
        {
            if (value.IsBool()) return value;

            return Expression.NotEqual(value, Expression.Constant(0UL));
        }

        private BitsValue GetConstantValue(LExpression expr) => GetConstantValue(Compile(expr, false));
        private static ulong GetConstantValue(Expression expr)
        {
            return Expression.Lambda<Func<ulong>>(expr)
#if USE_FAST_EXPRESSIONS
                .TryCompile<Func<ulong>>()();
#else
                .Compile()();
#endif
        }

        private static Expression Slice(Expression value, int start, int length)
        {
            return Expression.And(
                Expression.RightShift(value, Expression.Constant(start)),
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

        private static Expression BoolToNumberIf(Expression boolExpr, bool convert)
        {
            return convert ? Expression.Condition(boolExpr, Expression.Constant(1UL), Expression.Constant(0UL)) : boolExpr;
        }

        private Expression ReadInput(int start, int size)
        {
            return Expression.Field(
                Expression.Call(
                    Expression.Call(
                        Machine,
                        typeof(IMachine).GetMethod(nameof(IMachine.ReadInputs))
                    ),
                    typeof(BitsValue).GetMethod(nameof(BitsValue.Slice)),
                    Expression.Constant(start),
                    Expression.Constant(size)
                ),
                typeof(BitsValue).GetField(nameof(BitsValue.Number))
            );
        }
    }
}
