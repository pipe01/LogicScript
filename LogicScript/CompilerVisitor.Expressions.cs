using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using System;

namespace LogicScript
{
    internal partial class CompilerVisitor
    {
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

                case VariableAccessExpression var:
                    Visit(var);
                    break;

                case FunctionCallExpression funcCall:
                    Visit(funcCall);
                    break;
            }
        }

        private void Visit(FunctionCallExpression expr)
        {
            EmitFunctionCall(expr.Name, expr.Arguments, expr);
        }

        private void Visit(VariableAccessExpression expr)
        {
            var local = Local(expr.Name);
            Generator.Ldloca(local);
        }

        private void Visit(OperatorExpression expr)
        {
            var convert = true;

            if (expr.Operator == Operator.Assign)
            {
                VisitAssignment(expr.Left, expr.Right);
            }
            else if (expr.Operator == Operator.BitShiftLeft || expr.Operator == Operator.BitShiftRight)
            {
                DoBitshift();
                convert = false;
            }
            else if (!DoComparison())
            {
                Visit(expr.Left);
                BitsValueToNumber();
                Visit(expr.Right);
                BitsValueToNumber();

                //var label = Generator.DefineLabel("minmax");

                //Generator.Ldloc(Temp1);
                //Generator.Ldloc(Temp2);
                //Generator.Cgt(true); // Temp1 > Temp2
                //Generator.Brtrue(label);

                //// Temp2 > Temp1
                //Generator.Ldloc(Temp2);
                //Generator.Stloc(Temp1);

                //Generator.MarkLabel(label);

                switch (expr.Operator)
                {
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
                        Generator.And();
                        break;
                    case Operator.Or:
                        Generator.Or();
                        break;
                    case Operator.Xor:
                        Generator.Xor();
                        break;
                    case Operator.Not:
                        Generator.Not();
                        break;
                }
            }

            if (convert)
                NumberToBitsValue();

            void DoBitshift()
            {
                // Load left member (number), and store in Temp1
                Visit(expr.Left);
                Generator.Dup();
                Generator.Stloc(Temp1);
                BitsValueToNumber();

                // Load right member (shift amount), and store in Temp2
                Visit(expr.Right);
                BitsValueToNumber();
                Generator.Dup();
                Generator.Stloc(Temp2);

                // Do the shift
                if (expr.Operator == Operator.BitShiftLeft)
                    Generator.Shl();
                else
                    Generator.Shr(true);

                // Calculate result length (add or subtract shift amount from operand length)
                Generator.Ldloc(Temp1);
                ValueLength();
                Generator.Ldloc(Temp2);
                Generator.Conv<int>();

                if (expr.Operator == Operator.BitShiftLeft)
                    Generator.Add();
                else
                    Generator.Sub();

                NumberToBitsValue(true);
            }

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
            IndexerExpression indexer = null;

            if (left is IndexerExpression)
            {
                indexer = left as IndexerExpression;
                left = indexer.Operand;

                if (indexer.End != null)
                    throw new LogicEngineException("Indexers on left side of assignment cannot have an end position", indexer);
            }

            if (left is SlotExpression slotExpr)
            {
                if (slotExpr.Slot == Slots.Out)
                {
                    LoadMachine();

                    if (indexer == null)
                    {
                        Generator.Ldc_I4(0);
                    }
                    else
                    {
                        Visit(indexer.Start);
                        BitsValueToNumber();
                        Generator.Conv<int>();
                    }

                    Visit(right);
                    PointerToValue();
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
