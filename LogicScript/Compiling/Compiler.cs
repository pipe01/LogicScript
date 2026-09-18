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
using LogicScript.Parsing;
using System.Reflection;
using System.Reflection.Emit;
using Sigil.NonGeneric;
using Sigil;
using LogicScript.Parsing.Visitors;
using System.Diagnostics;

namespace LogicScript.Compiling
{
    public interface ICompiledScript
    {
        IRegisters Registers { get; }
        bool HasRun { get; set; }

        void Run(IMachine machine, IDebugger2? debugger = null);
    }

    public class Compiler
    {
        private enum Result
        {
            Empty,
        }

        private class Scope(IDictionary<LocalInfo, Local> locals)
        {
            public readonly IDictionary<LocalInfo, Local> Locals = locals;
        }

        private const int ArgumentThis = 0;
        private const int ArgumentMachine = 1;
        private const int ArgumentDebugger = 2;

        private readonly Script Script;
        private readonly bool EmitDebug;

        private readonly TypeBuilder TypeBuilder;
        private readonly FieldInfo HasRunField;
        private readonly FieldInfo RegistersField;

        private readonly Stack<Scope> Stack = new();
        private readonly Dictionary<NodeID, Sigil.Label> LoopBreaks = [];

        private readonly Emit Emitter;
        private readonly Local RegistersLocal;

        private Compiler(Script script, bool emitDebug)
        {
            this.Script = script;
            this.EmitDebug = emitDebug;

            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("<>ScriptAssembly"), AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule("Module");
            var tb = mb.DefineType("CompiledScript", TypeAttributes.Class);
            tb.AddInterfaceImplementation(typeof(ICompiledScript));
            TypeBuilder = tb;

            HasRunField = tb.DefineField("_hasRun", typeof(bool), FieldAttributes.Private);
            RegistersField = tb.DefineField("_registers", script.RegistersType, FieldAttributes.Private);

            tb.DefineProperty(nameof(ICompiledScript.Registers), typeof(IRegisters), RegistersField, false);
            tb.DefineProperty(nameof(ICompiledScript.HasRun), typeof(bool), HasRunField, true);

            var ctorMethod = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Type.EmptyTypes);
            var ctorIL = ctorMethod.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Newobj, script.RegistersType.GetConstructor(Type.EmptyTypes));
            ctorIL.Emit(OpCodes.Stfld, RegistersField);
            ctorIL.Emit(OpCodes.Ret);

            Emitter = Emit.BuildMethod(
                typeof(void),
                [typeof(IMachine), typeof(IDebugger2)],
                tb,
                nameof(ICompiledScript.Run),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot,
                CallingConventions.Standard | CallingConventions.HasThis
            );
            RegistersLocal = Emitter.DeclareLocal(script.RegistersType);
            Emitter.LoadArgument(ArgumentThis);
            Emitter.LoadField(RegistersField);
            Emitter.StoreLocal(RegistersLocal);
        }

        private void EmitThisField(FieldInfo field)
        {
            Emitter.LoadArgument(ArgumentThis);
            Emitter.LoadField(field);
        }

        private Result EmitConstant(BitsValue value)
        {
            Emitter.LoadConstant(value.Number);
            return Result.Empty;
        }

        private void DebugEmit(Action body)
        {
            if (!EmitDebug)
                return;

            var skip = Emitter.DefineLabel();

            Emitter.LoadArgument(ArgumentDebugger);
            Emitter.BranchIfFalse(skip);
            Emitter.LoadArgument(ArgumentDebugger);
            body();
            Emitter.MarkLabel(skip);
        }

        private void DebugEmitPushLocal(LocalInfo localInfo)
        {
            DebugEmit(() =>
            {
                Emitter.LoadConstant(localInfo.ID.ID);
                Emitter.NewObject<NodeID, int>();
                Emitter.CallVirtual(typeof(IDebugger2).GetMethod(nameof(IDebugger2.PushLocal)));
            });
        }

        private void DebugEmitPopLocal(LocalInfo localInfo)
        {
            DebugEmit(() =>
            {
                Emitter.LoadConstant(localInfo.ID.ID);
                Emitter.NewObject<NodeID, int>();
                Emitter.CallVirtual(typeof(IDebugger2).GetMethod(nameof(IDebugger2.PopLocal)));
            });
        }

        private void DebugEmitSetLocal(LocalInfo localInfo)
        {
            var local = FindLocal(localInfo);

            DebugEmit(() =>
            {
                Emitter.LoadConstant(localInfo.ID.ID);
                Emitter.NewObject<NodeID, int>();
                Emitter.LoadLocal(local);
                Emitter.CallVirtual(typeof(IDebugger2).GetMethod(nameof(IDebugger2.SetLocal)));
            });
        }

        private ICompiledScript Compile()
        {
            if (Script.HasErrors)
                throw new Exception("Script has errors");

            foreach (var block in Script.Blocks)
            {
                Compile(block);
            }

            Emitter.LoadArgument(ArgumentThis);
            Emitter.LoadConstant(true);
            Emitter.StoreField(HasRunField);
            Emitter.Return();

            Emitter.CreateMethod(out var str, OptimizationOptions.All);
            Debug.WriteLine(str);

            return (ICompiledScript)Activator.CreateInstance(TypeBuilder.CreateType());
        }

        public static ICompiledScript Compile(Script script, bool emitDebug = false)
        {
            return new Compiler(script, emitDebug).Compile();
        }

        private Result Compile(Block block)
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

        private Result Compile(Statement stmt)
        {
            DebugEmit(() =>
            {
                Emitter.LoadArgument(ArgumentThis);
                Emitter.LoadArgument(ArgumentMachine);
                Emitter.LoadConstant(stmt.ID.ID);
                Emitter.NewObject<NodeID, int>();
                Emitter.CallVirtual(typeof(IDebugger2).GetMethod(nameof(IDebugger2.TraceStatement)));
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
                _ => throw new NotImplementedException()
            };
        }

        private Result Compile(TaskStatement stmt)
        {
            switch (stmt)
            {
                case PrintTaskStatement print:
                    {
                        throw new NotImplementedException(); // TODO: implement
                        // Expression text;

                        // Emitter.LoadArgument(ArgumentMachine);

                        // if (print.String.Interpolations.Count == 0)
                        // {
                        //     Emitter.LoadConstant(print.String.Text);
                        // }
                        // else
                        // {
                        //     var locals = print.String.Interpolations.Select(l => FindLocal(l.Local));
                        //     var fmtString = print.String.ToFormattable();

                        //     // TODO: optimize this to make less allocations
                        //     text = Expression.Call(
                        //         typeof(string).GetMethod(nameof(string.Format), [typeof(string), typeof(object[])]),
                        //         [
                        //             Expression.Constant(fmtString),
                        //             Expression.NewArrayInit(typeof(object), locals.Select(l => Expression.Convert(l, typeof(object))))
                        //         ]
                        //     );
                        // }

                        // return Expression.Call(
                        //     ThisParameter,
                        //     typeof(IMachine).GetMethod(nameof(IMachine.Print)),
                        //     text
                        // );
                    }

                case ShowTaskStatement show:
                    Emitter.LoadArgument(ArgumentMachine);
                    Compile(show.Value);

                    using (var local = Emitter.DeclareLocal<ulong>())
                    {
                        Emitter.StoreLocal(local);
                        Emitter.LoadLocalAddress(local);
                        Emitter.Call(typeof(ulong).GetMethod(nameof(ToString), Type.EmptyTypes));
                        Emitter.CallVirtual(typeof(IMachine).GetMethod(nameof(IMachine.Print)));
                    }

                    return Result.Empty;

                case UpdateTaskStatement:
                    Emitter.LoadArgument(ArgumentMachine);
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

            var loopLocal = FindLocal(stmt.Variable);

            if (stmt.From != null)
                Compile(stmt.From);
            else
                EmitConstant(0);
            Emitter.StoreLocal(loopLocal);

            var body = Emitter.DefineLabel();
            var head = Emitter.DefineLabel();
            var afterLoop = Emitter.DefineLabel();

            Emitter.Branch(head);

            Emitter.MarkLabel(body);
            LoopBreaks[stmt.ID] = afterLoop;
            Compile(stmt.Body);
            LoopBreaks.Remove(stmt.ID);

            Emitter.LoadLocal(loopLocal);
            EmitConstant(1);
            Emitter.Add();
            Emitter.StoreLocal(loopLocal);
            DebugEmitSetLocal(stmt.Variable);

            Emitter.MarkLabel(head);
            Emitter.LoadLocal(loopLocal);
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
            var locals = stmt.Locals.ToDictionary(l => l, l => Emitter.DeclareLocal<ulong>(l.Name));

            foreach (var local in locals)
            {
                DebugEmitPushLocal(local.Key);
            }

            Stack.Push(new(locals));

            foreach (var child in stmt.Statements)
            {
                Compile(child);
            }

            var poppedScope = Stack.Pop();
            foreach (var local in poppedScope.Locals.Values)
            {
                local.Dispose();
            }
            foreach (var local in locals)
            {
                DebugEmitPopLocal(local.Key);
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
                            if (stmt.Value.BitSize == 1)
                            {
                                Emitter.LoadArgument(ArgumentMachine);
                                Emitter.LoadConstant(port.StartIndex); // TODO: vector
                                Compile(stmt.Value);
                                Emitter.CallVirtual(typeof(IMachine).GetMethod(nameof(IMachine.WriteOutput)));
                            }
                            else
                            {
                                Emitter.LoadArgument(ArgumentMachine);
                                Emitter.LoadConstant(port.StartIndex); // TODO: vector

                                Compile(stmt.Value);
                                Emitter.LoadConstant(port.BitSize);
                                Emitter.NewObject(typeof(BitsValue), [typeof(ulong), typeof(int)]);

                                Emitter.CallVirtual(typeof(IMachine).GetMethod(nameof(IMachine.WriteOutputs)));
                            }
                            return Result.Empty;

                        case MachinePorts.Register:
                            Emitter.LoadLocal(RegistersLocal);

                            var field = Script.RegistersType.GetField($"Register{port.PortInfo.StartIndex}");

                            if (port.VectorIndex != null)
                            {
                                Emitter.LoadField(field);
                                Compile(port.VectorIndex);
                                Compile(stmt.Value);
                                Emitter.Convert(field.FieldType);
                                Emitter.StoreElement(field.FieldType);
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
                        var localVar = FindLocal(local.LocalInfo);

                        Compile(stmt.Value);
                        Emitter.StoreLocal(localVar);

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

            var localVar = FindLocal(stmt.Local);

            Compile(stmt.Initializer);
            Emitter.StoreLocal(localVar);

            DebugEmitSetLocal(stmt.Local);

            return Result.Empty;
        }

        private Result Compile(LExpression expr)
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
                _ => throw new NotImplementedException()
            };
        }

        private Result Compile(TernaryOperatorExpression expr)
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

            return Result.Empty;
        }

        private Result Compile(SliceExpression expr)
        {
            // TODO: check that this is right

            Compile(expr.Operand);
            if (expr.Start == IndexStart.Right)
            {
                Compile(expr.Offset);
            }
            else
            {
                EmitConstant(expr.Operand.BitSize - expr.Length);
                Compile(expr.Offset);
                Emitter.Subtract();
            }
            Emitter.UnsignedShiftRight();

            Emitter.LoadConstant((1UL << expr.Length) - 1);
            Emitter.And();

            return Result.Empty;
        }

        private Result Compile(UnaryOperatorExpression expr)
        {
            switch (expr.Operator)
            {
                case Operator.Not:
                    Compile(expr.Operand);
                    Emitter.Not();
                    break;

                case Operator.Length:
                    EmitConstant(expr.Operand.BitSize);
                    break;

                case Operator.AllOnes:
                    Compile(expr.Operand);
                    EmitAllOnes(expr.Operand.BitSize);
                    break;

                default:
                    throw new InterpreterException("Unknown operand", expr.Span);
            }

            return Result.Empty;
        }

        private Result Compile(BinaryOperatorExpression expr)
        {
            switch (expr.Operator)
            {
                case Operator.And or Operator.Or or Operator.Xor or Operator.Add or Operator.Subtract or Operator.Multiply
                    or Operator.Divide or Operator.Modulus or Operator.EqualsCompare or Operator.NotEqualsCompare or Operator.Greater or Operator.Lesser:
                    Compile(expr.Left);
                    Compile(expr.Right);

                    _ = expr.Operator switch
                    {
                        Operator.And => Emitter.And(),
                        Operator.Or => Emitter.Or(),
                        Operator.Xor => Emitter.Xor(),
                        Operator.Add => Emitter.Add(),
                        Operator.Subtract => Emitter.Subtract(),
                        Operator.Multiply => Emitter.Multiply(),
                        Operator.Divide => Emitter.UnsignedDivide(),
                        Operator.Modulus => Emitter.Remainder(),
                        Operator.EqualsCompare => Emitter.CompareEqual(),
                        Operator.NotEqualsCompare => Emitter.CompareEqual().LoadConstant(0UL).CompareEqual(),
                        Operator.Greater => Emitter.CompareGreaterThan(),
                        Operator.Lesser => Emitter.CompareLessThan(),
                        _ => throw new NotImplementedException()
                    };
                    break;

                case Operator.ShiftLeft:
                    Compile(expr.Left);
                    Compile(expr.Right);
                    Emitter.Convert<int>();
                    Emitter.ShiftLeft();
                    break;

                case Operator.ShiftRight:
                    Compile(expr.Left);
                    Compile(expr.Right);
                    Emitter.Convert<int>();
                    Emitter.UnsignedShiftRight();
                    break;

                case Operator.Power:
                    throw new NotImplementedException("TODO: implement power");

                default:
                    throw new InterpreterException("Unknown operator", expr.Span);
            }

            return Result.Empty;
        }

        private Result Compile(ReferenceExpression expr)
        {
            switch (expr.Reference)
            {
                case LocalReference local:
                    var sLocal = FindLocal(local.LocalInfo);
                    Emitter.LoadLocal(sLocal);

                    return Result.Empty;

                case PortReference port:
                    switch (port.PortInfo.Target)
                    {
                        case MachinePorts.Input:
                            Emitter.LoadArgument(ArgumentMachine);
                            Emitter.LoadConstant(port.PortInfo.StartIndex);
                            // TODO: vector
                            Emitter.LoadConstant(port.PortInfo.BitSize);
                            Emitter.CallVirtual(typeof(IMachine).GetMethod(nameof(IMachine.ReadInputs)));
                            Emitter.LoadField(typeof(BitsValue).GetField(nameof(BitsValue.Number))); // TODO: make ReadInputs return a ulong directly
                            return Result.Empty;

                        case MachinePorts.Register:
                            Emitter.LoadLocal(RegistersLocal);

                            var field = Script.RegistersType.GetField($"Register{port.PortInfo.StartIndex}");
                            Emitter.LoadField(field);

                            var elemType = field.FieldType;
                            if (port.VectorIndex != null)
                            {
                                elemType = elemType.GetElementType();

                                Compile(port.VectorIndex);
                                Emitter.LoadElement(elemType);
                            }

                            if (elemType != typeof(ulong))
                                Emitter.Convert<ulong>();

                            return Result.Empty;

                        default:
                            throw new NotImplementedException();
                    }

                case ConstantReference cnst:
                    return EmitConstant(cnst.Constant.Value);

                default:
                    throw new NotImplementedException();
            }
        }

        private Result Compile(TruncateExpression expr)
        {
            ulong mask = 1UL << expr.BitSize;

            Compile(expr.Operand);
            EmitConstant(mask);
            Emitter.And();

            return Result.Empty;
        }

        private Local FindLocal(LocalInfo info)
        {
            foreach (var scope in Stack)
            {
                if (scope.Locals.TryGetValue(info, out var local))
                    return local;
            }

            throw new Exception($"Local {info} not found");
        }

        private Result EmitAllOnes(int length)
        {
            EmitConstant((1UL << length) - 1);
            Emitter.CompareEqual(); // TODO: this is an int32
            return Result.Empty;
        }

        // private Result ComputePortOffset(PortReference port)
        // {
        //     if (port.VectorIndex == null)
        //     {
        //         Emitter.LoadConstant(port.StartIndex);
        //     }
        //     else
        //     {
        //         if (port.VectorIndex.IsConstant)
        //         {
        //             var vectorIndex = (int)GetConstantValue(port.VectorIndex);
        //             return port.PortInfo.Target == MachinePorts.Register
        //                 ? Expression.Constant(port.StartIndex + vectorIndex)
        //                 : Expression.Constant(port.StartIndex + port.BitSize * vectorIndex);
        //         }
        //         else
        //         {
        //             return Expression.Add(
        //                 Expression.Constant(port.StartIndex),
        //                 port.PortInfo.Target == MachinePorts.Register
        //                     ? Compile(port.VectorIndex, false)
        //                     : Expression.Multiply(
        //                         Expression.Constant(port.BitSize),
        //                         Compile(port.VectorIndex, false)
        //                     )
        //             );
        //         }
        //     }
        // }
    }
}
