using GrEmit;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace LogicScript
{
    internal delegate void CaseDelegate(IMachine machine);

    internal static class Compiler
    {
        public static CaseDelegate CompileCases(ErrorSink errors, Case[] cases, string name)
        {
            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.RunAndCollect);
            var mb = ab.DefineDynamicModule("MainModule");
            var cb = mb.DefineType("Cases");

            var methods = new List<MethodInfo>();

            for (int i = 0; i < cases.Length; i++)
            {
                var method = cb.DefineMethod($"Case{i}", MethodAttributes.Private | MethodAttributes.Static, typeof(void), new[] { typeof(IMachine) });

                using (var il = new GroboIL(method))
                {
                    var visitor = new CompilerVisitor(il, errors);
                    visitor.Visit(cases[i]);

                    il.Ret();

                    //Console.WriteLine(il.GetILCode());
                }

                methods.Add(method);
            }

            CreateRunAllMethod(cb, methods);

            var type =
#if NETSTANDARD2_0
                cb.CreateTypeInfo();
#else
                cb.CreateType();
#endif

            return (CaseDelegate)type.GetMethod("RunAll").CreateDelegate(typeof(CaseDelegate));
        }

        private static void CreateRunAllMethod(TypeBuilder tb, IEnumerable<MethodInfo> methods)
        {
            var runAllMethod = tb.DefineMethod("RunAll", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] { typeof(IMachine) });

            using (var il = new GroboIL(runAllMethod))
            {
                foreach (var method in methods)
                {
                    il.Ldarg(0);
                    il.Call(method);
                }

                il.Ret();
            }
        }
    }
}
