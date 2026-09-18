using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using NUnit.Framework;

namespace LogicScript.Tests
{
    public class TestBlocks : BaseTest
    {
        [Test]
        public void Blocks_Print()
        {
            Run(new Script()
            {
                Blocks = {
                    new StartupBlock(default, new PrintTaskStatement(default, "nice1")),
                    new WhenBlock(default, null, new PrintTaskStatement(default, "nice2")),
                    new WhenBlock(default, new NumberLiteralExpression(default, 1), new PrintTaskStatement(default, "nice3")),
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
                    new StartupBlock(default, new PrintTaskStatement(default, "yes")),
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
                    new StartupBlock(default, new PrintTaskStatement(default, "yes")),
                }
            }, out var machine, runStartup: false);

            machine.AssertPrinted();
        }
    }
}