using LogicScript.Compiling;

namespace LogicScript.Tests
{
    public abstract class BaseTest
    {
        protected void Run(string source, DummyMachine machine, bool runStartup = true)
        {
            var (script, errors) = Script.Parse(source);
            if (errors.Count > 0)
                throw new System.Exception("Script has parsing errors: " + string.Join(", ", errors));

            Run(script!, machine, runStartup);
        }

        protected void Run(string source, out DummyMachine machine, bool runStartup = true)
        {
            machine = new DummyMachine();

            Run(source, machine, runStartup);
        }

        protected void Run(Script script, DummyMachine machine, bool runStartup = true)
        {
            var compiled = Compiler.Compile(script);
            compiled.Run(machine);
        }

        protected void Run(Script script, out DummyMachine machine, bool runStartup = true)
        {
            machine = new DummyMachine();

            Run(script, machine, runStartup);
        }
    }
}