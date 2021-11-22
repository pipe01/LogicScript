using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using GrEmit;
using LogicScript.Data;
using LogicScript.Parsing;

namespace LogicScript.Compiling
{
    internal delegate void ScriptDelegate(IMachine machine, bool runStartup);

    internal readonly ref partial struct Compiler
    {
        private readonly Script Script;
        private readonly GroboIL IL;
        private readonly GroboIL.Local Input;
        private readonly IDictionary<string, (GroboIL.Local Local, int Size)> Locals;

        public Compiler(Script script, GroboIL il)
        {
            this.Script = script;
            this.IL = il;
            this.Locals = new Dictionary<string, (GroboIL.Local, int)>();

            this.Input = il.DeclareLocal(typeof(Span<bool>), "input");

            LoadSpan<bool>(() =>
            {
                il.Ldarg(1); // Load machine
                il.Call(typeof(IMachine).GetProperty(nameof(IMachine.InputCount)).GetGetMethod());
            });
            il.Stloc(Input);

            LoadMachine();
            il.Ldc_I4(script.Registers.Count);
            il.Call(typeof(IMachine).GetMethod(nameof(IMachine.AllocateRegisters)));

            LoadMachine();
            il.Ldloc(Input);
            il.Call(typeof(IMachine).GetMethod(nameof(IMachine.ReadInput)));
        }

        private void LoadSpan<T>(Action loadLength)
        {
            loadLength();
            IL.Conv<UIntPtr>();
            IL.Localloc();
            loadLength();
            IL.Newobj(typeof(Span<T>).GetConstructor(new[] { typeof(void).MakePointerType(), typeof(int) }));
        }

        private void LoadNumber() => IL.Ldfld(typeof(BitsValue).GetField(nameof(BitsValue.Number)));

        private void LoadMachine() => IL.Ldarg(1);

        private void LoadBitsValue(BitsValue val)
        {
            IL.Ldc_I8((long)val.Number);
            IL.Conv<ulong>();
            IL.Ldc_I4(val.Length);
            IL.Newobj(typeof(BitsValue).GetConstructor(new[] { typeof(ulong), typeof(int) }));
        }

        private void LoadSpan(SourceSpan span)
        {
            IL.Ldc_I4(span.Start.Line);
            IL.Ldc_I4(span.Start.Column);
            IL.Ldc_I4(span.End.Line);
            IL.Ldc_I4(span.End.Column);
            IL.Newobj(typeof(SourceSpan).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(int), typeof(int), typeof(int), typeof(int) }, null));
        }

        public static ScriptDelegate Compile(Script script)
        {
            var ab = AssemblyBuilder.DefineDynamicAssembly(new("<>Script"), AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule("MainModule");
            var tb = mb.DefineType("Script",
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout);

            var scriptField = tb.DefineField("Script", typeof(Script), FieldAttributes.Private);

            var method = tb.DefineMethod("Run", MethodAttributes.Public, typeof(void), new[] { typeof(IMachine), typeof(bool) });

            using (var il = new GroboIL(method))
            {
                var compiler = new Compiler(script, il);

                foreach (var item in script.Blocks)
                {
                    compiler.Visit(item);
                }

                il.Ret();
            }

            var type = tb.CreateType();
            Debug.Assert(type != null);

            var inst = Activator.CreateInstance(type);
            inst.GetType().GetField("Script", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(inst, script);

            return (ScriptDelegate)type.GetMethod("Run", BindingFlags.Public | BindingFlags.Instance).CreateDelegate(typeof(ScriptDelegate), inst);
        }
    }
}