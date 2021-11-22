using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using NUnit.Framework;

namespace LogicScript.Tests
{
    public class TestBlocks : BaseTest
    {
        public TestBlocks(IRunner runner) : base(runner)
        {
        }

        [Test]
        public void Blocks_Print()
        {
            var machine = new DummyMachine();

            Runner.Run(new Script
            {
                Blocks = {
                    new StartupBlock(default, new PrintTaskStatement(default, "nice1")),
                    new WhenBlock(default, null, new PrintTaskStatement(default, "nice2")),
                    new WhenBlock(default, new NumberLiteralExpression(default, 1), new PrintTaskStatement(default, "nice3")),
                }
            }, machine);

            machine.AssertPrinted("nice1", "nice2", "nice3");
        }

        [Test]
        public void Startup_ShouldRun()
        {
            var machine = new DummyMachine();

            Runner.Run(new Script
            {
                Blocks = {
                    new StartupBlock(default, new PrintTaskStatement(default, "yes")),
                }
            }, machine, runStartup: true);

            machine.AssertPrinted("yes");
        }

        [Test]
        public void Startup_ShouldNotRun()
        {
            var machine = new DummyMachine();

            Runner.Run(new Script
            {
                Blocks = {
                    new StartupBlock(default, new PrintTaskStatement(default, "yes")),
                }
            }, machine, runStartup: false);

            machine.AssertPrinted();
        }
    }
}