using GrEmit;
using LogicScript.Parsing.Structures;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace LogicScript
{
    public class Compiler
    {
        public IEnumerable<Action<IMachine>> Compile(Script script)
        {
            foreach (var item in script.TopLevelNodes)
            {
                if (item is Case c)
                {
                    yield return CompileCase(c);
                }
            }
        }

        private Action<IMachine> CompileCase(Case c)
        {
            var method = new DynamicMethod($"<>{c.GetType().Name}", typeof(void), new[] { typeof(IMachine) });

            using (var il = new GroboIL(method))
            {
                var visitor = new CompilerVisitor(il);
                visitor.Visit(c.Statements);

                il.Ret();

                Console.WriteLine(il.GetILCode());
            }

            return (Action<IMachine>)method.CreateDelegate(typeof(Action<IMachine>));
        }
    }
}
