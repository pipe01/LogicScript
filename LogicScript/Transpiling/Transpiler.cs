using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FastExpressionCompiler.LightExpression;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using LogicScript.Parsing.Visitors;
using Expression = FastExpressionCompiler.LightExpression.Expression;
using LExpression = LogicScript.Parsing.Structures.Expressions.Expression;

namespace LogicScript.Transpiling
{
    public delegate void TranspiledScript(IMachine machine, BitsValue Input);

    public class Transpiler
    {
        private class Scope(IDictionary<LocalInfo, ParameterExpression> locals)
        {
            public readonly IDictionary<LocalInfo, ParameterExpression> Locals = locals;
        }

        private enum ValueKind
        {
            Bits,
            Bool,
        }

        private readonly ParameterExpression Machine = Expression.Parameter(typeof(IMachine), "machine");
        private readonly ParameterExpression Input = Expression.Parameter(typeof(BitsValue), "input");

        private readonly Stack<Scope> Stack = new();

        public TranspiledScript Transpile(Script script)
        {
            var expr = Expression.Block(script.Blocks.Select(Transpile));

            var ts = Expression.Lambda<TranspiledScript>(expr, Machine, Input).CompileFast(flags: CompilerFlags.EnableDelegateDebugInfo);

            TestTools.AllowPrintExpression = true;
            TestTools.AllowPrintCS = true;
            TestTools.AllowPrintIL = true;

            var d = ts.TryGetDebugInfo();
            d.PrintCSharp();
            d.PrintIL();

            return ts;
        }

        private Expression Transpile(Block block)
        {
            return block switch
            {
                StartupBlock sb => Transpile(sb),
                WhenBlock w => Transpile(w),
                _ => throw new NotImplementedException()
            };
        }

        private Expression Transpile(StartupBlock block)
        {
            return Transpile(block.Body);
        }

        private Expression Transpile(WhenBlock block)
        {
            var body = Transpile(block.Body);

            var isConstantlyTrue = block.Condition?.IsConstant == true && GetConstantValue(block.Condition).Number != 0;

            return block.Condition == null || isConstantlyTrue ? body : Expression.IfThen(
                IsTruthy(Transpile(block.Condition, true)),
                body
            );
        }

        private Expression Transpile(Statement stmt)
        {
            return stmt switch
            {
                AssignStatement a => Transpile(a),
                DeclareLocalStatement d => Transpile(d),
                BlockStatement b => Transpile(b),
                _ => throw new NotImplementedException()
            };
        }

        private Expression Transpile(BlockStatement stmt)
        {
            var locals = stmt.Locals.Select(l => (l.Value, Expression.Variable(typeof(BitsValue), l.Value.OriginalName))).ToDictionary(l => l.Value, l => l.Item2);

            Stack.Push(new(locals));

            var statements = stmt.Statements.Select(Transpile).ToArray();

            Stack.Pop();

            return Expression.Block(locals.Values, statements);
        }

        private Expression Transpile(AssignStatement stmt)
        {
            switch (stmt.Reference.Port)
            {
                case PortInfo port:
                    if (port.Target == MachinePorts.Output)
                    {
                        var value = Transpile(stmt.Value, port.BitSize == 1);

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
                                Expression.Property(value, typeof(BitsValue).GetProperty(nameof(BitsValue.BitsSpan)))
                            );
                        }
                    }
                    break;

                case LocalInfo local:
                    {
                        var localVar = FindLocal(local) ?? throw new Exception("local not found");
                        var value = Transpile(stmt.Value, false);

                        return Expression.Assign(localVar, value);
                    }
            }

            throw new NotImplementedException();
        }

        private Expression Transpile(DeclareLocalStatement stmt)
        {
            if (stmt.Initializer is null)
                return Expression.Block([]);

            var localVar = FindLocal(stmt.Local) ?? throw new Exception("local not found");

            return Expression.Assign(localVar, Transpile(stmt.Initializer, false));
        }

        private Expression Transpile(LExpression expr, bool canReturnBool)
        {
            return expr switch
            {
                BinaryOperatorExpression b => Transpile(b, canReturnBool),
                UnaryOperatorExpression u => Transpile(u, canReturnBool),
                NumberLiteralExpression n => canReturnBool ? Expression.Constant(n.Value != 0) : Expression.Constant(n.Value),
                ReferenceExpression r => Transpile(r, canReturnBool),
                TruncateExpression t => Transpile(t),
                _ => throw new NotImplementedException()
            };
        }

        private Expression Transpile(TruncateExpression expr)
        {
            var inner = Transpile(expr.Operand, false);

            return Expression.Call(
                inner,
                typeof(BitsValue).GetMethod(nameof(BitsValue.Resize)),
                Expression.Constant(expr.Size)
            );
        }

        private Expression Transpile(UnaryOperatorExpression expr, bool canReturnBool)
        {
            var inner = Transpile(expr.Operand, canReturnBool && expr.Operator == Operator.Not);

            return expr.Operator switch
            {
                Operator.Not => inner.IsBool()
                    ? Expression.Not(inner)
                    : Expression.Property(
                        inner,
                        typeof(BitsValue).GetProperty(nameof(BitsValue.Negated))
                    ),
                Operator.Rise => throw new NotImplementedException(),
                Operator.Fall => throw new NotImplementedException(),
                Operator.Change => throw new NotImplementedException(),
                Operator.Length => Expression.New(
                    typeof(BitsValue).GetConstructor([typeof(ulong), typeof(int)]),
                    Expression.Constant((ulong)expr.Operand.BitSize),
                    Expression.Constant(7)
                ),
                Operator.AllOnes => Expression.Convert(
                    Expression.Property(
                        inner,
                        typeof(BitsValue).GetProperty(nameof(BitsValue.AreAllBitsSet))
                    ),
                    typeof(BitsValue)
                ),
                _ => throw new InterpreterException("Unknown operand", expr.Span),
            };
        }

        private Expression Transpile(BinaryOperatorExpression expr, bool canReturnBool)
        {
            if (expr.Left.BitSize == 1 && expr.Right.BitSize == 1 && canReturnBool)
            {
                var leftBool = Transpile(expr.Left, true);
                var rightBool = Transpile(expr.Right, true);

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

            var left = Transpile(expr.Left, false);
            var right = Transpile(expr.Right, false);

            return Expression.Call(
                typeof(Operations).GetMethod(nameof(Operations.DoOperation)),
                left, right,
                Expression.Constant(expr.Operator)
            );
        }

        private Expression Transpile(ReferenceExpression expr, bool canReturnBool)
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
                        : Expression.Call(
                            Input,
                            typeof(BitsValue).GetMethod(nameof(BitsValue.Slice)),
                            Expression.Constant(port.StartIndex),
                            Expression.Constant(port.BitSize)
                        ),
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

            return Expression.NotEqual(
                Expression.Field(
                    value,
                    typeof(BitsValue).GetField(nameof(BitsValue.Number))
                ),
                Expression.Constant((ulong)0)
            );
        }

        private BitsValue GetConstantValue(LExpression expr) => GetConstantValue(Transpile(expr, false));
        private static BitsValue GetConstantValue(Expression expr)
        {
            return Expression.Lambda<Func<BitsValue>>(expr).TryCompile<Func<BitsValue>>()();
        }
    }
}
