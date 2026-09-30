using System;
using System.Collections.Generic;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing;
using System.Reflection;
using System.Reflection.Emit;
using Sigil.NonGeneric;
using System.Diagnostics;

#if NET5_0_OR_GREATER
using System.Runtime.Loader;
#endif

namespace LogicScript.Compiling
{
    [Flags]
    public enum Optimizations
    {
        ConstantFolding = 1 << 0,

        All = ConstantFolding,
        None = 0,
    }

    internal sealed class Compiler
    {
        private readonly Script Script;
        private readonly bool EmitDebug;
        private readonly Optimizations Optimizations;

#if NET5_0_OR_GREATER
        private readonly AssemblyLoadContext AssemblyLoadContext;
#endif

        private readonly ModuleBuilder ModuleBuilder;
        private readonly TypeBuilder TypeBuilder;
        private readonly RegistersStructBuilder RegistersStruct;

        private readonly FieldInfo HasRunField;
        private readonly FieldInfo MachineField;
        private readonly FieldInfo DebuggerField;
        private readonly ConstructorBuilder Constructor;

        private readonly Dictionary<NodeID, MethodCompiler> FunctionMethods = [];

        private Compiler(Script script, bool emitDebug, Optimizations optimizations
#if NET5_0_OR_GREATER
        , AssemblyLoadContext assemblyLoadContext
#endif
        )
        {
            this.Script = script;
            this.EmitDebug = emitDebug;
            this.Optimizations = optimizations;

#if NET5_0_OR_GREATER
            this.AssemblyLoadContext = assemblyLoadContext;
#endif

            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("<>ScriptAssembly"), AssemblyBuilderAccess.Run);
            ModuleBuilder = ab.DefineDynamicModule("Module");

            TypeBuilder = ModuleBuilder.DefineType("ICompiledScript", TypeAttributes.Class);
            TypeBuilder.AddInterfaceImplementation(typeof(IScriptInstance));

            RegistersStruct = new RegistersStructBuilder(TypeBuilder, [.. Script.Registers.Values]);
            RegistersStruct.GenerateMethods();

            HasRunField = TypeBuilder.DefineField("_hasRun", typeof(bool), FieldAttributes.Assembly);
            MachineField = TypeBuilder.DefineField("_machine", typeof(IMachine), FieldAttributes.Assembly);
            DebuggerField = TypeBuilder.DefineField("_debugger", typeof(IDebugger), FieldAttributes.Assembly);

            TypeBuilder.DefineProperty(nameof(IScriptInstance.HasRun), typeof(bool), HasRunField, true);
            TypeBuilder.DefineProperty(nameof(IScriptInstance.Machine), typeof(IMachine), MachineField, true);
            TypeBuilder.DefineProperty(nameof(IScriptInstance.Debugger), typeof(IDebugger), DebuggerField, true);

            var ctorEmit = Emit.BuildConstructor(Type.EmptyTypes, TypeBuilder, MethodAttributes.Public);
            RegistersStruct.EmitConstructorInit(ctorEmit);
            ctorEmit.LoadArgument(0);
            ctorEmit.Call(typeof(object).GetConstructor(Type.EmptyTypes));
            ctorEmit.Return();
            Constructor = ctorEmit.CreateConstructor();
        }

        private MethodCompiler CreateMethodCompiler(string methodName, Type returnType, LocalInfo[] parameters, bool isOverride)
        {
            var emitter = Emit.BuildInstanceMethod(
                returnType,
                parameters.ToIntegerTypes(),
                TypeBuilder,
                methodName,
                isOverride ? MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot : MethodAttributes.Private | MethodAttributes.HideBySig
            );

            return new MethodCompiler(
                emitter,
                [.. parameters],
                HasRunField,
                MachineField,
                DebuggerField,
                RegistersStruct,
                EmitDebug,
                FunctionMethods,
                Optimizations
            );
        }

        private ICompiledScript CreateFactory()
        {
            var tb = ModuleBuilder.DefineType("Factory", TypeAttributes.Class | TypeAttributes.Sealed);
            tb.AddInterfaceImplementation(typeof(ICompiledScript));

            var instEmitter = Emit.BuildInstanceMethod(
                typeof(IScriptInstance),
                [typeof(IMachine)],
                tb,
                nameof(ICompiledScript.Instantiate),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot
            );
            instEmitter.NewObject(Constructor);
            instEmitter.Duplicate();
            instEmitter.LoadArgument(1);
            instEmitter.StoreField(MachineField);
            instEmitter.Return();
            instEmitter.CreateMethod();

            var dispEmitter = Emit.BuildInstanceMethod(
                typeof(void),
                Type.EmptyTypes,
                tb,
                nameof(IDisposable.Dispose),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot
            );

#if NET5_0_OR_GREATER
            var alcField = tb.DefineField("ContainingLoadContext", typeof(AssemblyLoadContext), FieldAttributes.Private);
            dispEmitter.LoadArgument(0);
            dispEmitter.LoadField(alcField);
            dispEmitter.Call(typeof(AssemblyLoadContext).GetMethod(nameof(AssemblyLoadContext.Unload)));
#endif
            dispEmitter.Return();
            dispEmitter.CreateMethod();

            var type = tb.CreateType() ?? throw new Exception("Failed to create factory type");
            var instance = (ICompiledScript)(Activator.CreateInstance(type) ?? throw new Exception("Failed to create factory instance"));

#if NET5_0_OR_GREATER
            type.GetField(alcField.Name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(instance, AssemblyLoadContext);
#endif

            return instance;
        }

        private ICompiledScript Compile()
        {
            if (Script.HasErrors)
                throw new Exception("Script has errors");

            foreach (var func in Script.Functions.Values)
            {
                FunctionMethods[func.ID] = CreateMethodCompiler(func.Name, func.ResultType.ToIntegerType(), func.Parameters, false);
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

        public static ICompiledScript Compile(Script script, bool emitDebug = false, Optimizations optimizations = Optimizations.All)
        {
#if NET5_0_OR_GREATER
            var alc = new AssemblyLoadContext("ScriptLoadContext", true);
            using (alc.EnterContextualReflection())
            {
                return new Compiler(script, emitDebug, optimizations, alc).Compile();
            }
#else
            return new Compiler(script, emitDebug, optimizations).Compile();
#endif
        }
    }
}
