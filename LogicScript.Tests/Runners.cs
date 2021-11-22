using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogicScript.Compiling;

namespace LogicScript.Tests
{
    public class Runners : IEnumerable<object[]>
    {
        public static readonly IRunner Interpreted = new InterpretedRunner();
        public static readonly IRunner Compiled = new CompiledRunner();

        private static readonly object[] Data = new[] { new object[] { Interpreted }, new object[] { Compiled } };

        public IEnumerator<object[]> GetEnumerator() => ((IEnumerable<object[]>)Data).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public interface IRunner
    {
        void Run(Script script, IMachine machine);
    }

    public class InterpretedRunner : IRunner
    {
        public void Run(Script script, IMachine machine)
        {
            script.Run(machine, true);
        }
    }

    public class CompiledRunner : IRunner
    {
        public void Run(Script script, IMachine machine)
        {
            var del = Compiler.Compile(script);

            del(machine);
        }
    }
}