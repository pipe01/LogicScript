using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Statements;
using NUnit.Framework;

namespace LogicScript.Tests
{
    public class TestBlocks : BaseTest
    {
        [Test]
        public void Blocks_Print()
        {
            Run(@"
startup
    @print ""plain""

    local $a = 10
    @print $a

    @print ""dec: $a m""
    @print ""bin: $a:b m""
    @print ""hex: $a:x m""

    local $b = 321
    @print ""multiple: $a hello $b bye""
end
", out var machine);

            machine.AssertPrinted("plain", "10", "dec: 10 m", "bin: 1010 m", "hex: A m", "multiple: 10 hello 321 bye");
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