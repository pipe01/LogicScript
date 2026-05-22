using System;
using LogicScript.Compiling;

namespace LogicScript.Tests
{
    public static class Runners
    {
        public static readonly IRunner Interpreted = new InterpretedRunner();
        public static readonly IRunner Compiled = new CompiledRunner();

        public static readonly IRunner[] All = new[] { Interpreted, Compiled };
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
            var compiled = Compiler.Compile(script);

            compiled(machine, new bool[Math.Max(machine.InputCount, machine.OutputCount)], runStartup, null);
        }
    }
}