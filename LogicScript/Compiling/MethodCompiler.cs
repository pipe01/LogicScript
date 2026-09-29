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
using LogicScript.Parsing;
using System.Reflection;
using Sigil.NonGeneric;
using Sigil;
using LogicScript.Parsing.Visitors;
using System.Text;
using LogicScript.Utils;
using System.Reflection.Emit;
using System.Diagnostics;

namespace LogicScript.Compiling
{
    internal sealed class MethodCompiler(
        Emit Emitter,
        List<LocalInfo> Arguments,
        FieldInfo HasRunField,
        FieldInfo MachineField,
        FieldInfo DebuggerField,
        RegistersStructBuilder RegistersStruct,
        bool EmitDebug,
        Dictionary<NodeID, MethodCompiler> FunctionMethods
    )
    {
        public enum Result
        {
            Empty,
        }

        public readonly Emit Emitter = Emitter;

        private record Scope(IDictionary<LocalInfo, Local> Locals)
        {
            public bool HasReturned { get; set; }
        }

        private const int ArgumentThis = 0;

        private readonly Stack<Scope> Stack = new();
        private int LocalCounter;
        private readonly Dictionary<NodeID, Sigil.Label> LoopBreaks = [];

        private bool UsedHasRun;

        public MethodInfo Finish(bool setHasRun, bool addReturn)
        {
            if (setHasRun && UsedHasRun)
            {
                Emitter.LoadArgument(ArgumentThis);
                Emitter.LoadConstant(true);
                Emitter.StoreField(HasRunField);
            }

            if (addReturn)
                Emitter.Return();

            var method = Emitter.CreateMethod(out var insts);
            Debug.WriteLine(insts);

            return method;
        }

        private void EmitThisField(FieldInfo field)
        {
            Emitter.LoadArgument(ArgumentThis);
            Emitter.LoadField(field);
        }

        private Integer EmitConstant(BitsValue value) => EmitConstant(value, value.Length <= 32 ? Integer.Int : Integer.Long);
        private Integer EmitConstant(BitsValue value, Integer size)
        {
            if (size == Integer.Int)
            {
                Emitter.LoadConstant((uint)value.Number);
                return Integer.Int;
            }

            Emitter.LoadConstant(value.Number);
            return Integer.Long;
        }

        private void DebugEmit(Action body)
        {
            if (!EmitDebug)
                return;

            var skip = Emitter.DefineLabel();

            EmitThisField(DebuggerField);
            Emitter.BranchIfFalse(skip);
            EmitThisField(DebuggerField);
            body();
            Emitter.MarkLabel(skip);
        }

        private void DebugEmitPushLocal(LocalInfo localInfo)
        {
            DebugEmit(() =>
            {
                Emitter.LoadConstant(localInfo.ID.ID);
                Emitter.NewObject<NodeID, int>();
                Emitter.CallVirtual(typeof(IDebugger).GetMethod(nameof(IDebugger.PushLocal)));
            });
        }

        private void DebugEmitPopLocal(LocalInfo localInfo)
        {
            DebugEmit(() =>
            {
                Emitter.LoadConstant(localInfo.ID.ID);
                Emitter.NewObject<NodeID, int>();
                Emitter.CallVirtual(typeof(IDebugger).GetMethod(nameof(IDebugger.PopLocal)));
            });
        }

        private void DebugEmitSetLocal(LocalInfo localInfo)
        {
            DebugEmit(() =>
            {
                Emitter.LoadConstant(localInfo.ID.ID);
                Emitter.NewObject<NodeID, int>();
                LoadLocal(localInfo);
                Emitter.CallVirtual(typeof(IDebugger).GetMethod(nameof(IDebugger.SetLocal)));
            });
        }

        public void DebugEmitFunctionStart(FunctionBlock function)
        {
            DebugEmit(() =>
            {
                Emitter.LoadConstant(function.ID.ID);
                Emitter.NewObject<NodeID, int>();
                Emitter.CallVirtual(typeof(IDebugger).GetMethod(nameof(IDebugger.PushFunctionCall)));
            });

            foreach (var param in function.Parameters)
            {
                DebugEmitPushLocal(param);
                DebugEmitSetLocal(param);
            }
        }

        public Result Compile(Block block)
        {
            return block switch
            {
                StartupBlock sb => Compile(sb),
                WhenBlock w => Compile(w),
                AssignBlock b => Compile((Statement)b.Assignment), // Cast to Statement to call the overload that adds debug instrumentation
                _ => throw new NotImplementedException()
            };
        }

        private Result Compile(StartupBlock block)
        {
            UsedHasRun = true;

            var end = Emitter.DefineLabel();

            EmitThisField(HasRunField);
            Emitter.BranchIfTrue(end);

            Compile(block.Body);

            Emitter.MarkLabel(end);

            return Result.Empty;
        }

        private Result Compile(WhenBlock block)
        {
            if (block.Condition == null || (block.Condition.IsConstant == true && block.Condition.GetConstantValue() != 0))
            {
                // Always true

                Compile(block.Body);
                return Result.Empty;
            }

            var whenFalse = Emitter.DefineLabel();

            Compile(block.Condition);
            Emitter.BranchIfFalse(whenFalse);
            Compile(block.Body);
            Emitter.MarkLabel(whenFalse);

            return Result.Empty;
        }

        public Result Compile(Statement stmt)
        {
            DebugEmit(() =>
            {
                Emitter.LoadArgument(ArgumentThis);
                EmitThisField(MachineField);
                Emitter.LoadConstant(stmt.ID.ID);
                Emitter.NewObject<NodeID, int>();
                Emitter.CallVirtual(typeof(IDebugger).GetMethod(nameof(IDebugger.TraceStatement)));
            });

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
                ReturnStatement r => Compile(r),
                _ => throw new NotImplementedException()
            };
        }

        private Result Compile(TaskStatement stmt)
        {
            switch (stmt)
            {
                case PrintTaskStatement print:
                    {
                        EmitThisField(MachineField);

                        if (print.String.Parts.Count == 0)
                        {
                            Emitter.LoadConstant(print.String.Text + "\n");
                        }
                        else
                        {
                            using var builder = Emitter.DeclareLocal<StringBuilder>();

                            Emitter.NewObject<StringBuilder>();
                            Emitter.StoreLocal(builder);

                            foreach (var part in print.String.Parts)
                            {
                                Emitter.LoadLocal(builder);

                                if (part is PrintStringFormat.PartLiteral lit)
                                {
                                    Emitter.LoadConstant(lit.String);
                                }
                                else if (part is PrintStringFormat.PartInterpolate interp)
                                {
                                    LoadLocal(interp.LocalInfo, address: true);
                                    Emitter.LoadConstant(interp.Format switch
                                    {
                                        PrintStringFormat.NumberFormat.Hexadecimal => "X",
                                        PrintStringFormat.NumberFormat.Binary => "b",
                                        _ => "",
                                    });

                                    var intType = interp.LocalInfo.BitSize.ToIntegerSize().ToIntegerType();
                                    Emitter.Call(intType.GetMethod(nameof(ToString), [typeof(string)]));
                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }

                                Emitter.Call(typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), [typeof(string)]));
                                Emitter.Pop();
                            }

                            Emitter.LoadLocal(builder);
                            Emitter.Call(typeof(StringBuilder).GetMethod(nameof(StringBuilder.AppendLine), Type.EmptyTypes));
                            Emitter.Call(typeof(StringBuilder).GetMethod(nameof(StringBuilder.ToString), Type.EmptyTypes));
                        }

                        Emitter.CallVirtual(typeof(IMachine).GetMethod(nameof(IMachine.Print)));

                        return Result.Empty;
                    }

                case ShowTaskStatement show:
                    EmitThisField(MachineField);
                    Compile(show.Value);

                    var valueType = show.Value.ResultType.ToIntegerType();
                    using (var local = Emitter.DeclareLocal(valueType))
                    {
                        Emitter.StoreLocal(local);
                        Emitter.LoadLocalAddress(local);
                        Emitter.Call(valueType.GetMethod(nameof(ToString), Type.EmptyTypes));
                        Emitter.CallVirtual(typeof(IMachine).GetMethod(nameof(IMachine.PrintLine)));
                    }

                    return Result.Empty;

                case UpdateTaskStatement:
                    EmitThisField(MachineField);
                    Emitter.CallVirtual(typeof(IMachine).GetMethod(nameof(IMachine.QueueUpdate)));

                    return Result.Empty;
            }

            throw new NotImplementedException();
        }

        private Result Compile(BreakStatement stmt)
        {
            var label = LoopBreaks[stmt.TargetID]; // No need to check presence, the parser takes care of it

            Emitter.Branch(label);
            return Result.Empty;
        }

        private Result Compile(ForStatement stmt)
        {
            //TODO: optimize: compute 'to' once on loop enter and don't recompute on each iteration

            if (stmt.From != null)
                Compile(stmt.From);
            else
                EmitConstant(0);
            StoreLocal(stmt.Variable);

            var body = Emitter.DefineLabel();
            var head = Emitter.DefineLabel();
            var afterLoop = Emitter.DefineLabel();

            Emitter.Branch(head);

            Emitter.MarkLabel(body);
            LoopBreaks[stmt.ID] = afterLoop;
            Compile(stmt.Body);
            LoopBreaks.Remove(stmt.ID);

            LoadLocal(stmt.Variable);
            EmitConstant(1);
            Emitter.Add();
            StoreLocal(stmt.Variable);
            DebugEmitSetLocal(stmt.Variable);

            Emitter.MarkLabel(head);
            LoadLocal(stmt.Variable);
            Compile(stmt.To);
            Emitter.BranchIfLess(body);

            Emitter.MarkLabel(afterLoop);

            return Result.Empty;
        }

        private Result Compile(WhileStatement stmt)
        {
            var startLabel = Emitter.DefineLabel();
            var breakLabel = Emitter.DefineLabel();

            Emitter.MarkLabel(startLabel);
            Compile(stmt.Condition);
            Emitter.BranchIfFalse(breakLabel);

            LoopBreaks[stmt.ID] = breakLabel;
            Compile(stmt.Body);
            LoopBreaks.Remove(stmt.ID);

            Emitter.Branch(startLabel);
            Emitter.MarkLabel(breakLabel);

            return Result.Empty;
        }

        private Result Compile(IfStatement stmt)
        {
            if (stmt.Condition.IsConstant)
            {
                var condConst = stmt.Condition.GetConstantValue();

                if (condConst != 0)
                    return Compile(stmt.Body);
                else if (stmt.Else != null)
                    return Compile(stmt.Else);
                else
                    return Result.Empty;
            }

            var falseLabel = Emitter.DefineLabel();

            Compile(stmt.Condition);
            Emitter.BranchIfFalse(falseLabel);
            Compile(stmt.Body);

            if (stmt.Else != null)
            {
                var endLabel = Emitter.DefineLabel();

                Emitter.Branch(endLabel);
                Emitter.MarkLabel(falseLabel);
                Compile(stmt.Else);
                Emitter.MarkLabel(endLabel);
            }
            else
            {
                Emitter.MarkLabel(falseLabel);
            }

            return Result.Empty;
        }

        private Result Compile(BlockStatement stmt)
        {
            var locals = stmt.Locals.ToDictionary(l => l, l => Emitter.DeclareLocal(l.BitSize.ToIntegerSize().ToIntegerType(), $"{l.Name}_{LocalCounter++}"));

            foreach (var local in locals)
            {
                DebugEmitPushLocal(local.Key);
            }

            Stack.Push(new(locals));

            foreach (var child in stmt.Statements)
            {
                Compile(child);
            }

            var poppedStack = Stack.Pop();
            foreach (var local in poppedStack.Locals)
            {
                local.Value.Dispose();
            }

            // If the block statement's body directly contains a return statement, the compiler will emit a "pop function"
            // debug call which will clear all locals, so we don't need to individually emit "pop local" calls.
            if (EmitDebug && !poppedStack.HasReturned)
            {
                foreach (var local in poppedStack.Locals)
                {
                    DebugEmitPopLocal(local.Key);
                }
            }

            return Result.Empty;
        }

        private Result Compile(AssignStatement stmt)
        {
            switch (stmt.Reference)
            {
                case PortReference port:
                    switch (port.PortInfo.Target)
                    {
                        case MachinePorts.Output:
                            EmitThisField(MachineField);
                            Emitter.LoadConstant(port.StartIndex);
                            if (port.VectorIndex != null)
                            {
                                Emitter.LoadConstant(port.PortInfo.BitSize);
                                var indexType = Compile(port.VectorIndex);
                                Coerce(indexType, Integer.Int);
                                Emitter.Multiply();
                                Emitter.Add();
                            }
                            var valueType = Compile(stmt.Value);

                            if (port.BitSize == 1)
                            {
                                Emitter.Convert<bool>();
                                Emitter.CallVirtual(typeof(IMachine).GetMethod(nameof(IMachine.WriteOutput)));
                            }
                            else
                            {
                                Coerce(valueType, Integer.Long);
                                EmitConstant(port.BitSize, Integer.Int);
                                Emitter.NewObject(typeof(BitsValue), [typeof(ulong), typeof(int)]);

                                Emitter.CallVirtual(typeof(IMachine).GetMethod(nameof(IMachine.WriteOutputs)));
                            }
                            return Result.Empty;

                        case MachinePorts.Register:
                            Emitter.LoadArgument(ArgumentThis);

                            var field = RegistersStruct.Registers[port.PortInfo].Field;

                            if (port.VectorIndex != null)
                            {
                                Emitter.LoadField(field);
                                Compile(port.VectorIndex);
                                Emitter.Convert<int>();
                                Compile(stmt.Value);
                                Emitter.Convert(field.FieldType.GetElementType());
                                Emitter.StoreElement(field.FieldType.GetElementType());
                            }
                            else
                            {
                                Compile(stmt.Value);
                                Emitter.Convert(field.FieldType);
                                Emitter.StoreField(field);
                            }
                            return Result.Empty;
                    }
                    throw new NotImplementedException();

                case LocalReference local:
                    {

                        Compile(stmt.Value);
                        StoreLocal(local.LocalInfo);

                        DebugEmitSetLocal(local.LocalInfo);

                        return Result.Empty;
                    }
            }

            throw new NotImplementedException();
        }

        private Result Compile(DeclareLocalStatement stmt)
        {
            if (stmt.Initializer is null)
                return Result.Empty;

            Compile(stmt.Initializer);
            StoreLocal(stmt.Local);

            DebugEmitSetLocal(stmt.Local);

            return Result.Empty;
        }

        private Result Compile(ReturnStatement stmt)
        {
            Compile(stmt.Value);

            DebugEmit(() => Emitter.CallVirtual(typeof(IDebugger).GetMethod(nameof(IDebugger.PopFunctionCall))));

            Emitter.Return();

            Stack.Peek().HasReturned = true;

            return Result.Empty;
        }

        private Integer Compile(Expression expr)
        {
            if (expr.IsConstant)
                return EmitConstant(expr.GetConstantValue());

            return expr switch
            {
                BinaryOperatorExpression b => Compile(b),
                NumberLiteralExpression n => EmitConstant(n.Value),
                ReferenceExpression r => Compile(r),
                SliceExpression s => Compile(s),
                TernaryOperatorExpression t => Compile(t),
                TruncateExpression t => Compile(t),
                UnaryOperatorExpression u => Compile(u),
                ReferenceLengthExpression r => EmitConstant((ulong)r.Value),
                FunctionCallExpression c => Compile(c),
                _ => throw new NotImplementedException()
            };
        }

        private Integer Compile(TernaryOperatorExpression expr)
        {
            if (expr.Condition.IsConstant)
            {
                var condConst = expr.Condition.GetConstantValue();

                if (condConst != 0)
                    return Compile(expr.IfTrue);
                else
                    return Compile(expr.IfFalse);
            }

            var ifFalse = Emitter.DefineLabel();
            var end = Emitter.DefineLabel();

            Compile(expr.Condition);
            Emitter.BranchIfFalse(ifFalse);
            Compile(expr.IfTrue);
            Emitter.Branch(end);
            Emitter.MarkLabel(ifFalse);
            Compile(expr.IfFalse);
            Emitter.MarkLabel(end);

            return expr.ResultType;
        }

        private Integer Compile(SliceExpression expr)
        {
            // TODO: check that this is right

            var operandType = Compile(expr.Operand);

            if (expr.Start == IndexStart.Right)
            {
                Compile(expr.Offset);
            }
            else
            {
                EmitConstant(expr.Operand.BitSize - expr.Length, expr.Offset.ResultType);
                Compile(expr.Offset);
                Emitter.Subtract();
            }
            Emitter.UnsignedShiftRight();

            EmitConstant((1UL << expr.Length) - 1, operandType);
            Emitter.And();

            return expr.ResultType;
        }

        private Integer Compile(UnaryOperatorExpression expr)
        {
            switch (expr.Operator)
            {
                case UnaryOperator.Not:
                    Compile(expr.Operand);
                    Emitter.Not();
                    break;

                case UnaryOperator.Length:
                    EmitConstant(expr.Operand.BitSize);
                    break;

                case UnaryOperator.AllOnes:
                    // TODO: optimize: add short-circuiting when doing allOnes of inputs

                    Compile(expr.Operand);
                    EmitAllOnes(expr.Operand.BitSize);
                    break;

                default:
                    throw new InterpreterException("Unknown operand", expr.Span);
            }

            return expr.ResultType;
        }

        private Integer Compile(BinaryOperatorExpression expr)
        {
            switch (expr.Operator)
            {
                case BinaryOperator.And or BinaryOperator.Or or BinaryOperator.Xor or BinaryOperator.Add or BinaryOperator.Subtract or BinaryOperator.Multiply
                    or BinaryOperator.Divide or BinaryOperator.Modulus or BinaryOperator.EqualsCompare or BinaryOperator.NotEqualsCompare or BinaryOperator.Greater or BinaryOperator.Lesser:
                    var leftType = Compile(expr.Left);
                    Coerce(leftType, expr.ResultType);

                    var rightType = Compile(expr.Right);
                    Coerce(rightType, expr.ResultType);

                    _ = expr.Operator switch
                    {
                        BinaryOperator.And => Emitter.And(),
                        BinaryOperator.Or => Emitter.Or(),
                        BinaryOperator.Xor => Emitter.Xor(),
                        BinaryOperator.Add => Emitter.Add(),
                        BinaryOperator.Subtract => Emitter.Subtract(),
                        BinaryOperator.Multiply => Emitter.Multiply(),
                        BinaryOperator.Divide => Emitter.UnsignedDivide(),
                        BinaryOperator.Modulus => Emitter.Remainder(),
                        BinaryOperator.EqualsCompare => Emitter.CompareEqual(),
                        BinaryOperator.NotEqualsCompare => Emitter.CompareEqual().LoadConstant(0UL).CompareEqual(),
                        BinaryOperator.Greater => Emitter.CompareGreaterThan(),
                        BinaryOperator.Lesser => Emitter.CompareLessThan(),
                        _ => throw new NotImplementedException()
                    };
                    break;

                case BinaryOperator.ShiftLeft:
                    leftType = Compile(expr.Left);
                    Coerce(leftType, expr.ResultType); // shifting left over the 32-bit boundary produces a 64-bit number

                    rightType = Compile(expr.Right);
                    Coerce(rightType, Integer.Int);

                    Emitter.ShiftLeft();
                    break;

                case BinaryOperator.ShiftRight:
                    Compile(expr.Left);

                    rightType = Compile(expr.Right);
                    Coerce(rightType, Integer.Int);

                    Emitter.UnsignedShiftRight();
                    break;

                case BinaryOperator.AndAlso or BinaryOperator.OrElse:
                    {
                        bool isAnd = expr.Operator == BinaryOperator.AndAlso;

                        var end = Emitter.DefineLabel();
                        var shortcut = Emitter.DefineLabel();

                        Compile(expr.Left);
                        if (isAnd)
                            Emitter.BranchIfFalse(shortcut);
                        else
                            Emitter.BranchIfTrue(shortcut);

                        Compile(expr.Right);
                        Emitter.Branch(end);

                        Emitter.MarkLabel(shortcut);
                        EmitConstant(isAnd ? 0 : 1, expr.ResultType);

                        Emitter.MarkLabel(end);
                    }
                    break;

                case BinaryOperator.Power:
                    throw new NotImplementedException("TODO: implement power");

                default:
                    throw new InterpreterException("Unknown operator", expr.Span);
            }

            return expr.ResultType;
        }

        private Integer Compile(ReferenceExpression expr)
        {
            switch (expr.Reference)
            {
                case LocalReference local:
                    LoadLocal(local.LocalInfo);
                    break;

                case PortReference port:
                    switch (port.PortInfo.Target)
                    {
                        case MachinePorts.Input:
                            EmitThisField(MachineField);
                            Emitter.LoadConstant(port.PortInfo.StartIndex);
                            if (port.VectorIndex != null)
                            {
                                Emitter.LoadConstant(port.PortInfo.BitSize);
                                Compile(port.VectorIndex);
                                Emitter.Convert<int>();
                                Emitter.Multiply();
                                Emitter.Add();
                            }
                            Emitter.LoadConstant(port.PortInfo.BitSize);
                            Emitter.CallVirtual(typeof(IMachine).GetMethod(nameof(IMachine.ReadInputs)));
                            Emitter.LoadField(typeof(BitsValue).GetField(nameof(BitsValue.Number))); // TODO: make ReadInputs return a ulong directly
                            Coerce(Integer.Long, expr.ResultType);
                            break;

                        case MachinePorts.Register:
                            Emitter.LoadArgument(ArgumentThis);

                            var field = RegistersStruct.Registers[port.PortInfo].Field;
                            Emitter.LoadField(field);

                            var elemType = field.FieldType;
                            if (port.VectorIndex != null)
                            {
                                elemType = elemType.GetElementType();

                                Compile(port.VectorIndex);
                                Emitter.Convert<int>();
                                Emitter.LoadElement(elemType);
                            }
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                    break;

                case ConstantReference cnst:
                    return EmitConstant(cnst.Constant.Value);

                default:
                    throw new NotImplementedException();
            }

            return expr.ResultType;
        }

        private Integer Compile(TruncateExpression expr)
        {
            ulong mask = (1UL << expr.BitSize) - 1;

            var operandType = Compile(expr.Operand);
            EmitConstant(mask, operandType);
            Emitter.And();

            return expr.ResultType;
        }

        private Integer Compile(FunctionCallExpression expr)
        {
            if (!FunctionMethods.TryGetValue(expr.Function.ID, out var method))
                throw new Exception($"Function implementation for {expr.Function.Name} not found");

            Emitter.LoadArgument(ArgumentThis);
            foreach (var arg in expr.Arguments)
            {
                Compile(arg);
            }

            var innerEmit = typeof(Emit).GetField("InnerEmit", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(method.Emitter);
            var methodBuilder = (MethodBuilder)innerEmit.GetType().GetProperty("MtdBuilder", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(innerEmit);

            Emitter.Call(new MethodInfoProxy(methodBuilder, typeof(ulong), [.. Enumerable.Repeat(typeof(ulong), expr.Function.Parameters.Length)]));

            return expr.ResultType;
        }

        private void LoadLocal(LocalInfo info, bool address = false)
        {
            foreach (var scope in Stack)
            {
                if (scope.Locals.TryGetValue(info, out var local))
                {
                    if (address)
                        Emitter.LoadLocalAddress(local);
                    else
                        Emitter.LoadLocal(local);
                    return;
                }
            }

            int argIndex = Arguments.FindIndex(i => i.Equals(info));
            if (argIndex >= 0)
            {
                if (address)
                    Emitter.LoadArgumentAddress((ushort)(argIndex + 1));
                else
                    Emitter.LoadArgument((ushort)(argIndex + 1));
                return;
            }

            throw new Exception($"Local {info} not found");
        }

        private void StoreLocal(LocalInfo info)
        {
            foreach (var scope in Stack)
            {
                if (scope.Locals.TryGetValue(info, out var local))
                {
                    Emitter.StoreLocal(local);
                    return;
                }
            }

            int argIndex = Arguments.FindIndex(i => i.Equals(info));
            if (argIndex >= 0)
            {
                Emitter.StoreArgument((ushort)argIndex);
                return;
            }

            throw new Exception($"Local {info} not found");
        }

        private Result EmitAllOnes(int length)
        {
            EmitConstant((1UL << length) - 1);
            Emitter.CompareEqual(); // TODO: this is an int32
            return Result.Empty;
        }

        private void Coerce(Integer current, Integer wanted)
        {
            if (current != wanted)
            {
                switch (wanted)
                {
                    case Integer.Int:
                        Emitter.Convert<int>();
                        break;
                    case Integer.Long:
                        Emitter.Convert<long>();
                        break;
                    default:
                        throw new ArgumentException(nameof(wanted));
                }
            }
        }
    }
}
