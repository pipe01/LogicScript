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
        void Run(Script script, IMachine machine, bool runStartup = true);
    }

    public class InterpretedRunner : IRunner
    {
        public void Run(Script script, IMachine machine, bool runStartup = true)
        {
            script.Run(machine, runStartup);
        }
    }

    public class CompiledRunner : IRunner
    {
        public void Run(Script script, IMachine machine, bool runStartup = true)
        {
            var del = Compiler.Compile(script);

            del(machine, runStartup);
        }
    }
}