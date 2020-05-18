using LogicScript.Data;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using System;
using System.Reflection.Emit;
using Index = LogicScript.Parsing.Structures.Index;

namespace LogicScript
{
    internal partial class CompilerVisitor
    {
        private void Visit(Expression expr)
        {
            Generator.Nop();

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

                case IndexerExpression indexer when indexer.Operand is SlotExpression slot:
                    Visit(slot, indexer.Index);
                    break;
                    
                case RangeExpression range when range.Operand is SlotExpression slot:
                    Visit(slot, range.Start, range.End);
                    break;

                case SlotExpression slotExpr:
                    Visit(slotExpr);
                    break;

                case UnaryOperatorExpression unary:
                    Visit(unary);
                    break;

                default:
                    throw new LogicEngineException("Invalid expression", expr);
            }
        }

        private void Visit(SlotExpression expr, Index start = default, Index? end = null)
        {
            LoadMachine();
            if (expr.Slot == Slots.Memory)
                LoadMemory();

            LoadIndex(expr, start);

            // Calculate range length
            if (end == null)
            {
                Generator.Ldc_I4(1);
            }
            else
            {
                Generator.Dup(); // Duplicate start index for GetInputs parameter

                // length = -start + end
                Generator.Neg();

                LoadIndex(expr, end.Value);

                Generator.Add();
            }

            if (expr.Slot == Slots.In)
                Generator.Call(Info.OfMethod<IMachine>(nameof(IMachine.GetInputs), "System.Int32,System.Int32"));
            else if (expr.Slot == Slots.Memory)
                Generator.Call(Info.OfMethod<IMemory>(nameof(IMemory.Read), "System.Int32,System.Int32"));
            else
                throw new LogicEngineException("Invalid slot", expr);

            ValueToReference();
        }

        private void GetSlotSize(SlotExpression expr)
        {
            LoadMachine();
            if (expr.Slot == Slots.In)
            {
                Generator.Call(Info.OfPropertyGet<IMachine>(nameof(IMachine.InputCount)));
            }
            else if (expr.Slot == Slots.Memory)
            {
                LoadMemory();
                Generator.Call(Info.OfPropertyGet<IMemory>(nameof(IMemory.Capacity)));
            }
            else
            {
                throw new LogicEngineException("Invalid slot", expr);
            }
        }

        private void LoadIndex(SlotExpression expr, Index idx)
        {
            if (idx.FromEnd)
                GetSlotSize(expr);

            if (idx.Value == null)
            {
                Generator.Ldc_I4(0);
            }
            else
            {
                Visit(idx.Value);
                BitsValueToNumber();
                Generator.Conv<int>();
            }

            if (idx.FromEnd)
                Generator.Sub();
        }

        private void Visit(UnaryOperatorExpression expr)
        {
            Visit(expr.Operand);

            switch (expr.Operator)
            {
                case Operator.Not:
                    Generator.Call(Info.OfPropertyGet<BitsValue>(nameof(BitsValue.Negated)));
                    ValueToReference();
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
            Index? start = null;

            if (left is RangeExpression range)
            {
                start = range.Start;
                left = range.Operand;

                if (range.End != Index.End)
                    throw new LogicEngineException("Indexers on left side of assignment cannot have an end position", left);
            }
            else if (left is IndexerExpression indexer)
            {
                start = indexer.Index;
                left = indexer.Operand;
            }

            if (left is SlotExpression slotExpr)
            {
                LoadMachine();
                if (slotExpr.Slot == Slots.Memory)
                    LoadMemory();

                if (start == null)
                    Generator.Ldc_I4(0);
                else
                    LoadIndex(slotExpr, start.Value);

                Visit(right);
                PointerToValue();

                if (slotExpr.Slot == Slots.Out)
                {
                    Generator.Call(typeof(IMachine).GetMethod(nameof(IMachine.SetOutputs)));
                }
                else if (slotExpr.Slot == Slots.Memory)
                {
                    Generator.Call(Info.OfMethod<IMemory>(nameof(IMemory.Write), "System.Int32,LogicScript.Data.BitsValue"));
                }
            }
            else if (left is VariableAccessExpression varExpr)
            {
                if (start != null)
                    throw new LogicEngineException("Variable assignments cannot be indexer", left);

                var local = Local(varExpr.Name);

                Visit(right);
                Generator.Stloc(local);
            }
            else
            {
                throw new LogicEngineException("Invalid expression", left);
            }

            Generator.Ldc_I8(0);
            Generator.Conv<ulong>();
        }
    }
}
