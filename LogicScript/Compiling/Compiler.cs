using System;
using System.Collections.Generic;
using System.Linq;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing;
using System.Reflection;
using System.Reflection.Emit;
using Sigil.NonGeneric;
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

        private readonly Dictionary<NodeID, MethodCompiler> FunctionMethods = [];

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

        private MethodCompiler CreateMethodCompiler(string methodName, Type returnType, LocalInfo[] parameters, bool isOverride)
        {
            var emitter = Emit.BuildInstanceMethod(
                returnType,
                [.. Enumerable.Repeat(typeof(ulong), parameters.Length)],
                TypeBuilder,
                methodName,
                isOverride ? MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot : MethodAttributes.Private | MethodAttributes.HideBySig
            );

            return new(
                Script,
                emitter,
                [.. parameters],
                HasRunField,
                RegistersField,
                MachineField,
                DebuggerField,
                EmitDebug,
                FunctionMethods
            );
        }

        private CompiledScript Compile()
        {
            if (Script.HasErrors)
                throw new Exception("Script has errors");

            foreach (var func in Script.Functions.Values)
            {
                FunctionMethods[func.ID] = CreateMethodCompiler(func.Name, typeof(ulong), func.Parameters, false);
            }

            foreach (var func in Script.Functions.Values)
            {
                var methodCompiler = FunctionMethods[func.ID];

                Debug.Assert(func.Body != null);

                methodCompiler.Compile(func.Body);
                methodCompiler.Finish(false, false);
            }

            var runMethodCompiler = CreateMethodCompiler(nameof(IScriptInstance.Run), typeof(void), [], true);
            foreach (var block in Script.Blocks)
            {
                runMethodCompiler.Compile(block);
            }
            runMethodCompiler.Finish(true, true);

            return new(TypeBuilder.CreateType());
        }

        public static CompiledScript Compile(Script script, bool emitDebug = false)
        {
            return new Compiler(script, emitDebug).Compile();
        }
    }
}
