using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogicScript.Compiling;

namespace LogicScript.Tests
{
    public static class Runners
    {
        public static readonly IRunner Interpreted = new InterpretedRunner();
        public static readonly IRunner Compiled = new CompiledRunner();

        public static readonly IRunner[] All = new[] { Interpreted , Compiled };
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