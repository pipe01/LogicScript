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
                foreach (var stmt in c.Statements)
                {
                    var visitor = new Visitor(il);
                    visitor.VisitStatement(stmt);
                }

                il.Ret();
            }

            return (Action<IMachine>)method.CreateDelegate(typeof(Action<IMachine>));
        }

        private class Visitor
        {
            private static readonly ConstructorInfo BitsValueCtor = typeof(BitsValue).GetConstructor(new[] { typeof(uint) });

            private readonly GroboIL Generator;

            public Visitor(GroboIL generator)
            {
                this.Generator = generator ?? throw new ArgumentNullException(nameof(generator));
            }

            public void VisitStatement(Statement statement)
            {
                switch (statement)
                {
                    case ExpressionStatement expr:
                        Visit(expr.Expression);
                        Generator.Pop();
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            private void LoadMachine() => Generator.Ldarg(0);

            private void LoadValue(BitsValue value)
            {
                Generator.Ldc_I8((long)value.Number);

                //if (!asNumber)
                //    ConvertBitsValue(true);
            }

            private void ConvertBitsValue(bool convUlong = false)
            {
                if (convUlong)
                    Generator.Conv<ulong>();

                Generator.Newobj(BitsValueCtor);
            }

            private void Visit(Expression expr)
            {
                switch (expr)
                {
                    case NumberLiteralExpression literal:
                        LoadValue(literal.Value);
                        break;

                    case OperatorExpression opExpr:
                        Visit(opExpr);
                        break;
                }
            }

            private void Visit(OperatorExpression expr)
            {
                if (expr.Operator == Operator.Assign)
                {
                    VisitAssignment(expr.Left, expr.Right);
                    return;
                }

                Visit(expr.Left);
                Visit(expr.Right);

                switch (expr.Operator)
                {
                    case Operator.NotEquals:
                        break;
                    case Operator.Equals:
                        break;
                    case Operator.Greater:
                        break;
                    case Operator.GreaterOrEqual:
                        break;
                    case Operator.Lesser:
                        break;
                    case Operator.LesserOrEqual:
                        break;
                    case Operator.BitShiftLeft:
                        break;
                    case Operator.BitShiftRight:
                        break;
                    case Operator.Add:
                        Generator.Add();
                        ConvertBitsValue();
                        break;
                    case Operator.Subtract:
                        Generator.Sub();
                        ConvertBitsValue();
                        break;
                    case Operator.Modulo:
                        Generator.Rem(true);
                        break;
                    case Operator.Multiply:
                        Generator.Mul();
                        ConvertBitsValue();
                        break;
                    case Operator.Divide:
                        Generator.Div(true);
                        ConvertBitsValue();
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

            private void VisitAssignment(Expression left, Expression right)
            {
                if (left is SlotExpression slotExpr)
                {
                    if (slotExpr.Slot == Slots.Out)
                    {
                        LoadMachine();
                        Visit(right);
                        Generator.Call(typeof(IMachine).GetMethod(nameof(IMachine.SetOut)));
                    }
                }

                Visit(right);
            }
        }
    }
}

#endif