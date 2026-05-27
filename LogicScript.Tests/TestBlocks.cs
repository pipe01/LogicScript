using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using NUnit.Framework;

namespace LogicScript.Tests
{
    public class TestBlocks(RunnerType runnerType) : BaseTest(runnerType)
    {
        [Test]
        public void Blocks_Print()
        {
            Run(new Script()
            {
                Blocks = {
                    new StartupBlock(default, new PrintTaskStatement("nice1")),
                    new WhenBlock(default, null, new PrintTaskStatement("nice2")),
                    new WhenBlock(default, new NumberLiteralExpression(default, 1), new PrintTaskStatement("nice3")),
                }
            }, out var machine);

            machine.AssertPrinted("nice1", "nice2", "nice3");
        }

        [Test]
        public void Startup_ShouldRun()
        {
            Run(new Script()
            {
                Blocks = {
                    new StartupBlock(default, new PrintTaskStatement("yes")),
                }
            }, out var machine, runStartup: true);

            machine.AssertPrinted("yes");
        }

        [Test]
        public void Startup_ShouldNotRun()
        {
            Run(new Script()
            {
                Blocks = {
                    new StartupBlock(default, new PrintTaskStatement("yes")),
                }
            }, out var machine, runStartup: false);

            machine.AssertPrinted();
        }
    }
}