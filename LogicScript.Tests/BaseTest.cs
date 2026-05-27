using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace LogicScript.Tests
{
    public enum RunnerType
    {
        Interpreted,
        Compiled,
    }

    [TestFixtureSource(nameof(Data))]
    public abstract class BaseTest(RunnerType runnerType)
    {

        public static readonly IEnumerable<object> Data = [RunnerType.Interpreted, RunnerType.Compiled];

        protected readonly bool Interpreted = runnerType == RunnerType.Interpreted;

        protected void Run(Script script, DummyMachine machine, bool runStartup = true)
        {
            if (Interpreted)
                Runner.Interpreted(null).Run(machine, script, runStartup);
            else
                Runner.Compiled().Run(machine, script, runStartup);
        }

        protected void Run(Script script, out DummyMachine machine, bool runStartup = true)
        {
            machine = new DummyMachine();

            Run(script, machine, runStartup);
        }

        [SetUp]
        public void Setup()
        {
            TestContext.Write("Running using " + (Interpreted ? "interpreted" : "compiled") + " runner");
        }
    }
}