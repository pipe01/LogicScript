#if !NETSTANDARD2_0

using GrEmit;
using LogicScript.Data;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace LogicScript
{
    public class Compiler
    {
        private readonly Script Script;

        public void Compile(Script script, IMachine machine)
        {
            foreach (var item in script.TopLevelNodes)
            {
                if (item is Case c)
                {
                    var a = CompileCase(c);
                    a(machine);
                }
            }
        }

        private Action<IMachine> CompileCase(Case c)
        {
            var method = new DynamicMethod($"<>{c.GetType().Name}", typeof(void), new[] { typeof(IMachine) });

            using (var il = new GroboIL(method))
            {
                var visitor = new Visitor(il);
                visitor.Visit(c.Statements);

                il.Ret();

                Console.WriteLine(il.GetILCode());
            }

            return (Action<IMachine>)method.CreateDelegate(typeof(Action<IMachine>));
        }

        private class Visitor
        {
            private static readonly ConstructorInfo BitsValueCtor = typeof(BitsValue).GetConstructor(new[] { typeof(uint) });
            private static readonly FieldInfo BitsValueNumber = typeof(BitsValue).GetField(nameof(BitsValue.Number));
            private static readonly MethodInfo BitsValueIsTruthy = typeof(BitsValue).GetProperty(nameof(BitsValue.IsAnyBitSet)).GetMethod;

            private readonly GroboIL Generator;
            private readonly ILGenerator RawGenerator;
            private readonly IDictionary<string, GroboIL.Local> Locals = new Dictionary<string, GroboIL.Local>();

            public Visitor(GroboIL generator)
            {
                this.Generator = generator ?? throw new ArgumentNullException(nameof(generator));
                this.RawGenerator = (ILGenerator)typeof(GroboIL).GetField("il", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(generator);
            }

            public void Visit(IReadOnlyList<Statement> statements)
            {
                foreach (var stmt in statements)
                {
                    Visit(stmt);
                }
            }

            private void Visit(Statement statement)
            {
                switch (statement)
                {
                    case ExpressionStatement expr:
                        Visit(expr.Expression);
                        Generator.Pop();
                        break;

                    case IfStatement ifStmt:
                        Visit(ifStmt);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            private void Visit(IfStatement stmt)
            {
                Visit(stmt.Condition);
                BitsValueToNumber();

                if (stmt.Else == null)
                {
                    var bodyEndLabel = Generator.DefineLabel("body");

                    Generator.Brfalse(bodyEndLabel);

                    Visit(stmt.Body);
                    Generator.MarkLabel(bodyEndLabel);
                }
                else
                {
                    var mainStartLabel = Generator.DefineLabel("main");
                    var mainEndLabel = Generator.DefineLabel("else");

                    Generator.Brtrue(mainStartLabel);

                    Visit(stmt.Else);
                    Generator.Br(mainEndLabel);

                    Generator.MarkLabel(mainStartLabel);
                    Visit(stmt.Body);
                    Generator.MarkLabel(mainEndLabel);
                }
            }

            private GroboIL.Local Local(string name)
            {
                if (!Locals.TryGetValue(name, out var local))
                {
                    Locals[name] = local = Generator.DeclareLocal(typeof(BitsValue), name);
                }

                return local;
            }

            private void LoadMachine() => Generator.Ldarg(0);

            private void LoadValue(BitsValue value) => Generator.Ldc_I8((long)value.Number);

            private void NumberToBitsValue() => Generator.Newobj(BitsValueCtor);

            private void BitsValueToNumber() => Generator.Ldfld(BitsValueNumber);

            private void Visit(Expression expr)
            {
                switch (expr)
                {
                    case NumberLiteralExpression literal:
                        LoadValue(literal.Value);
                        NumberToBitsValue();
                        break;

                    case OperatorExpression opExpr:
                        Visit(opExpr);
                        break;

                    case VariableAccessExpression var:
                        Visit(var);
                        break;
                }
            }

            private void Visit(VariableAccessExpression expr)
            {
                var local = Local(expr.Name);
                Generator.Ldloc(local);
            }

            private void Visit(OperatorExpression expr)
            {
                var convert = true;

                if (expr.Operator == Operator.Assign)
                {
                    VisitAssignment(expr.Left, expr.Right);
                }
                else if (!DoComparison())
                {
                    Visit(expr.Left);
                    Visit(expr.Right);

                    switch (expr.Operator)
                    {
                        case Operator.BitShiftLeft:
                            Generator.Shl();
                            break;
                        case Operator.BitShiftRight:
                            Generator.Shr(true);
                            break;
                        case Operator.Add:
                            Generator.Add();
                            break;
                        case Operator.Subtract:
                            Generator.Sub();
                            break;
                        case Operator.Modulo:
                            Generator.Rem(true);
                            break;
                        case Operator.Multiply:
                            Generator.Mul();
                            break;
                        case Operator.Divide:
                            Generator.Div(true);
                            break;
                        case Operator.And:
                            
                            break;
                        case Operator.Or:
                            break;
                        case Operator.Xor:
                            break;
                        case Operator.Not:
                            break;
                        case Operator.Truncate:
                            break;
                        default:
                            break;
                    }
                }

                if (convert)
                    NumberToBitsValue();

                bool DoComparison()
                {
                    switch (expr.Operator)
                    {
                        case Operator.NotEquals:
                            EmitCompare(() => Generator.Ldc_I4(1), () =>
                            {
                                Generator.Ceq();
                                Generator.Sub();
                            });
                            break;

                        case Operator.Equals:
                            EmitCompare(null, () => Generator.Ceq());
                            break;

                        case Operator.Greater:
                            EmitCompare(null, () => Generator.Cgt(true));
                            break;

                        case Operator.GreaterOrEqual:
                            EmitCompare(null, () =>
                            {
                                Generator.Clt(true);
                                Generator.Ldc_I4(0);
                                Generator.Ceq();
                            });
                            break;
                            
                        case Operator.Lesser:
                            EmitCompare(null, () => Generator.Clt(true));
                            break;

                        case Operator.LesserOrEqual:
                            EmitCompare(null, () =>
                            {
                                Generator.Cgt(true);
                                Generator.Ldc_I4(0);
                                Generator.Ceq();
                            });
                            break;

                        default:
                            return false;
                    }

                    return true;
                }

                void EmitCompare(Action before, Action after)
                {
                    before?.Invoke();

                    Visit(expr.Left);
                    BitsValueToNumber();

                    Visit(expr.Right);
                    BitsValueToNumber();

                    after?.Invoke();
                    Generator.Conv<ulong>();
                }
            }

            private void VisitAssignment(Expression left, Expression right)
            {
                Expression start = null;

                if (left is IndexerExpression indexer)
                {
                    start = indexer.Start;
                    left = indexer.Operand;
                }

                if (left is SlotExpression slotExpr)
                {
                    if (slotExpr.Slot == Slots.Out)
                    {
                        LoadMachine();

                        if (start == null)
                        {
                            Generator.Ldc_I4(0);
                        }
                        else
                        {
                            Visit(start);
                            Generator.Conv<int>();
                        }

                        Visit(right);
                        Generator.Call(typeof(IMachine).GetMethod(nameof(IMachine.SetOutputs)));
                    }
                    else if (slotExpr.Slot == Slots.Memory)
                    {

                    }
                }
                else if (left is VariableAccessExpression varExpr)
                {
                    var local = Local(varExpr.Name);

                    Visit(right);
                    Generator.Stloc(local);
                }

                Generator.Ldc_I8(0);
                Generator.Conv<ulong>();
            }
        }
    }
}

#endif