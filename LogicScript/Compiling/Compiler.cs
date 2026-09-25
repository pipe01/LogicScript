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
    public sealed class Compiler
    {
        private readonly Script Script;
        private readonly bool EmitDebug;

        private readonly Type RegistersType;

        private readonly ModuleBuilder ModuleBuilder;
        private readonly TypeBuilder TypeBuilder;

        private readonly FieldInfo HasRunField;
        private readonly FieldInfo RegistersField;
        private readonly FieldInfo MachineField;
        private readonly FieldInfo DebuggerField;
        private readonly ConstructorBuilder Constructor;

        private readonly Dictionary<NodeID, MethodCompiler> FunctionMethods = [];

        private Compiler(Script script, bool emitDebug)
        {
            this.Script = script;
            this.EmitDebug = emitDebug;

            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("<>ScriptAssembly"), AssemblyBuilderAccess.Run);
            ModuleBuilder = ab.DefineDynamicModule("Module");
            TypeBuilder = ModuleBuilder.DefineType("ICompiledScript", TypeAttributes.Class);
            TypeBuilder.AddInterfaceImplementation(typeof(IScriptInstance));

            this.RegistersType = RegistersStruct.Generate(ModuleBuilder, [.. Script.Registers.Values]);

            HasRunField = TypeBuilder.DefineField("_hasRun", typeof(bool), FieldAttributes.Assembly);
            RegistersField = TypeBuilder.DefineField("_registers", RegistersType, FieldAttributes.Assembly);
            MachineField = TypeBuilder.DefineField("_machine", typeof(IMachine), FieldAttributes.Assembly);
            DebuggerField = TypeBuilder.DefineField("_debugger", typeof(IDebugger), FieldAttributes.Assembly);

            TypeBuilder.DefineProperty(nameof(IScriptInstance.Registers), typeof(IRegisters), RegistersField, false);
            TypeBuilder.DefineProperty(nameof(IScriptInstance.HasRun), typeof(bool), HasRunField, true);
            TypeBuilder.DefineProperty(nameof(IScriptInstance.Machine), typeof(IMachine), MachineField, true);
            TypeBuilder.DefineProperty(nameof(IScriptInstance.Debugger), typeof(IDebugger), DebuggerField, true);

            Constructor = TypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Type.EmptyTypes);
            var ctorIL = Constructor.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Newobj, RegistersType.GetConstructor(Type.EmptyTypes));
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

            return new MethodCompiler(
                RegistersType,
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

        private ICompiledScript CreateFactory()
        {
            var tb = ModuleBuilder.DefineType("Factory", TypeAttributes.Class | TypeAttributes.Sealed);
            tb.AddInterfaceImplementation(typeof(ICompiledScript));

            var emitter = Emit.BuildInstanceMethod(
                typeof(IScriptInstance),
                [typeof(IMachine)],
                tb,
                nameof(ICompiledScript.Instantiate),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot
            );
            emitter.NewObject(Constructor);
            emitter.Duplicate();
            emitter.LoadArgument(1);
            emitter.StoreField(MachineField);
            emitter.Return();
            emitter.CreateMethod();

            var type = tb.CreateType();

            return (ICompiledScript)Activator.CreateInstance(type);
        }

        private ICompiledScript Compile()
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

                methodCompiler.DebugEmitFunctionStart(func);

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

            TypeBuilder.CreateType();

            return CreateFactory();
        }

        public static ICompiledScript Compile(Script script, bool emitDebug = false)
        {
            return new Compiler(script, emitDebug).Compile();
        }
    }
}
