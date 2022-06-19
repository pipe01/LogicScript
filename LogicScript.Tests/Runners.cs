using LogicScript.ByteCode;

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
            var bytecode = Compiler.Compile(script);

            new CPU(bytecode, machine).Run(true);
        }
    }
}