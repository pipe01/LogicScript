using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using GrEmit;

namespace LogicScript.Compiling
{
    internal delegate void ScriptDelegate(IMachine machine);

    internal readonly ref partial struct Compiler
    {
        private readonly Script Script;
        private readonly GroboIL IL;

        public Compiler(Script script, GroboIL iL)
        {
            this.Script = script;
            this.IL = iL;
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

            var method = tb.DefineMethod("Run", MethodAttributes.Public, typeof(void), new[] { typeof(IMachine) });

            using (var il = new GroboIL(method))
            {
                var compiler = new Compiler(script, il);

                il.Ldarg(1);
                il.Ldstr("nice");
                il.Call(typeof(IMachine).GetMethod(nameof(IMachine.Print)));
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