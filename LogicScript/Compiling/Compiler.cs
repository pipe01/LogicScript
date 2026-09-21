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
using System.Reflection.Emit;
using Sigil.NonGeneric;
using Sigil;
using LogicScript.Parsing.Visitors;
using System.Text;
using LogicScript.Utils;
using System.Diagnostics;

namespace LogicScript.Compiling
{
    public sealed class CompiledScript
    {
        private readonly Type Type;

        internal CompiledScript(Type type)
        {
            this.Type = type;
        }

        public IScriptInstance Instantiate(IMachine machine)
        {
            var instance = (IScriptInstance)Activator.CreateInstance(Type);
            instance.Machine = machine;
            return instance;
        }
    }

    public sealed class Compiler
    {
        private readonly Script Script;
        private readonly bool EmitDebug;

        private readonly TypeBuilder TypeBuilder;
        private readonly FieldInfo HasRunField;
        private readonly FieldInfo RegistersField;
        private readonly FieldInfo MachineField;
        private readonly FieldInfo DebuggerField;

        private Compiler(Script script, bool emitDebug)
        {
            this.Script = script;
            this.EmitDebug = emitDebug;

            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("<>ScriptAssembly"), AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule("Module");
            var tb = mb.DefineType("CompiledScript", TypeAttributes.Class);
            tb.AddInterfaceImplementation(typeof(IScriptInstance));
            TypeBuilder = tb;

            HasRunField = tb.DefineField("_hasRun", typeof(bool), FieldAttributes.Private);
            RegistersField = tb.DefineField("_registers", script.RegistersType, FieldAttributes.Private);
            MachineField = tb.DefineField("_machine", typeof(IMachine), FieldAttributes.Private);
            DebuggerField = tb.DefineField("_debugger", typeof(IDebugger), FieldAttributes.Private);

            tb.DefineProperty(nameof(IScriptInstance.Registers), typeof(IRegisters), RegistersField, false);
            tb.DefineProperty(nameof(IScriptInstance.HasRun), typeof(bool), HasRunField, true);
            tb.DefineProperty(nameof(IScriptInstance.Machine), typeof(IMachine), MachineField, true);
            tb.DefineProperty(nameof(IScriptInstance.Debugger), typeof(IDebugger), DebuggerField, true);

            var ctorMethod = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Type.EmptyTypes);
            var ctorIL = ctorMethod.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Newobj, script.RegistersType.GetConstructor(Type.EmptyTypes));
            ctorIL.Emit(OpCodes.Stfld, RegistersField);
            ctorIL.Emit(OpCodes.Ret);

        }

        private MethodCompiler CreateMethodCompiler(string methodName, Type returnType, Type[] parameters, bool isOverride)
        {
            var emitter = Emit.BuildMethod(
                returnType,
                parameters,
                TypeBuilder,
                methodName,
                isOverride ? MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot : MethodAttributes.Private,
                CallingConventions.Standard | CallingConventions.HasThis
            );

            return new(
                Script,
                emitter,
                HasRunField,
                RegistersField,
                MachineField,
                DebuggerField,
                EmitDebug
            );
        }

        private CompiledScript Compile()
        {
            if (Script.HasErrors)
                throw new Exception("Script has errors");

            foreach (var func in Script.Functions.Values)
            {
                var methodCompiler = CreateMethodCompiler(func.Name, typeof(ulong), [.. Enumerable.Repeat(typeof(ulong), func.Parameters.Length)], false);
                methodCompiler.Compile(func.Body);
                methodCompiler.Finish(false);
            }

            var runMethodCompiler = CreateMethodCompiler(nameof(IScriptInstance.Run), typeof(void), Type.EmptyTypes, true);
            foreach (var block in Script.Blocks)
            {
                runMethodCompiler.Compile(block);
            }
            runMethodCompiler.Finish(true);

            return new(TypeBuilder.CreateType());
        }

        public static CompiledScript Compile(Script script, bool emitDebug = false)
        {
            return new Compiler(script, emitDebug).Compile();
        }
    }
}
