using GrEmit;
using LogicScript.Parsing.Structures;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace LogicScript
{
    internal delegate void CaseDelegate(IMachine machine);

    internal static class Compiler
    {
        public static IEnumerable<CaseDelegate> Compile(Script script)
        {
            foreach (var item in script.TopLevelNodes)
            {
                if (item is Case c)
                {
                    yield return CompileCase(c);
                }
            }
        }

        public static CaseDelegate CompileCase(Case c)
        {
            var method = new DynamicMethod($"<>{c.GetType().Name}", typeof(void), new[] { typeof(IMachine) });

            using (var il = new GroboIL(method))
            {
                var visitor = new CompilerVisitor(il);
                visitor.Visit(c.Statements);

                il.Ret();

                Console.WriteLine(il.GetILCode());
            }

            return (CaseDelegate)method.CreateDelegate(typeof(CaseDelegate));
        }
    }
}
