using LogicScript.Compiling;

namespace LogicScript.Tests
{
    public abstract class BaseTest
    {
        protected static void Run(string source, DummyMachine machine, byte[]? registers = null, bool runStartup = true)
        {
            var (script, errors) = Script.Parse(source);
            if (errors.Count > 0)
                throw new System.Exception("Script has parsing errors: " + string.Join(", ", errors));

            Run(script!, machine, registers, runStartup);
        }

        protected static void Run(string source, out DummyMachine machine, byte[]? registers = null, bool runStartup = true)
        {
            machine = new DummyMachine();

            Run(source, machine, registers, runStartup);
        }

        protected static void Run(Script script, DummyMachine machine, byte[]? registers = null, bool runStartup = true)
        {
            var instance = Compiler.Compile(script).Instantiate(machine);
            instance.DecodeRegisters(registers);
            instance.HasRun = !runStartup;
            instance.Run();
        }

        protected static void Run(Script script, out DummyMachine machine, byte[]? registers = null, bool runStartup = true)
        {
            machine = new DummyMachine();

            Run(script, machine, registers, runStartup);
        }
    }
}