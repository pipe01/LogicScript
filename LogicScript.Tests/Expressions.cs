using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using NUnit.Framework;

namespace LogicScript.Tests
{
    public class RunBlocks : BaseTest
    {
        public RunBlocks(IRunner runner) : base(runner)
        {
        }

        [Test]
        public void Print_Startup()
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
        public void Print_AddLiterals()
        {
            var machine = new DummyMachine();

            Runner.Run(new Script
            {
                Blocks = {
                    new StartupBlock(default,
                        new ShowTaskStatement(default,
                            new BinaryOperatorExpression(default, Operator.Add,
                                new NumberLiteralExpression(default, 1),
                                new NumberLiteralExpression(default, 2)
                            )
                        )
                    ),
                }
            }, machine);

            machine.AssertPrinted("3");
        }

        [Test]
        public void Print_AddInputs()
        {
            var machine = new DummyMachine(new[] { true, true, false });

            var a = new PortInfo(MachinePorts.Input, 0, 1, default);
            var b = new PortInfo(MachinePorts.Input, 1, 2, default);

            Runner.Run(new Script
            {
                Inputs = {
                    { "a", a },
                    { "b", b },
                },
                Blocks = {
                    new StartupBlock(default,
                        new ShowTaskStatement(default,
                            new BinaryOperatorExpression(default, Operator.Add,
                                new ReferenceExpression(default, new PortReference(default, a)),
                                new ReferenceExpression(default, new PortReference(default, b))
                            )
                        )
                    ),
                }
            }, machine);

            machine.AssertPrinted("3");
        }

        [Test]
        public void Print_LiteralSliceRight()
        {
            var machine = new DummyMachine();

            Runner.Run(new Script
            {
                Blocks = {
                    new StartupBlock(default,
                        new ShowTaskStatement(default,
                            new SliceExpression(default,
                                new NumberLiteralExpression(default, 13),
                                IndexStart.Right,
                                new NumberLiteralExpression(default, 1),
                                2
                            )
                        )
                    )
                }
            }, machine);

            machine.AssertPrinted("2");
        }

        [Test]
        public void Print_LiteralSliceLeft()
        {
            var machine = new DummyMachine();

            Runner.Run(new Script
            {
                Blocks = {
                    new StartupBlock(default,
                        new ShowTaskStatement(default,
                            new SliceExpression(default,
                                new NumberLiteralExpression(default, 13),
                                IndexStart.Left,
                                new NumberLiteralExpression(default, 1),
                                2
                            )
                        )
                    )
                }
            }, machine);

            machine.AssertPrinted("2");
        }

        [Test]
        public void Print_TernaryTrue()
        {
            var machine = new DummyMachine();

            Runner.Run(new Script
            {
                Blocks = {
                    new StartupBlock(default,
                        new ShowTaskStatement(default,
                            new TernaryOperatorExpression(default,
                                new NumberLiteralExpression(default, 1),
                                new NumberLiteralExpression(default, 10),
                                new NumberLiteralExpression(default, 20)
                            )
                        )
                    )
                }
            }, machine);

            machine.AssertPrinted("10");
        }

        [Test]
        public void Print_TernaryFalse()
        {
            var machine = new DummyMachine();

            Runner.Run(new Script
            {
                Blocks = {
                    new StartupBlock(default,
                        new ShowTaskStatement(default,
                            new TernaryOperatorExpression(default,
                                new NumberLiteralExpression(default, 0),
                                new NumberLiteralExpression(default, 10),
                                new NumberLiteralExpression(default, 20)
                            )
                        )
                    )
                }
            }, machine);

            machine.AssertPrinted("20");
        }

        [Test]
        public void Print_Invert()
        {
            var machine = new DummyMachine();

            Runner.Run(new Script
            {
                Blocks = {
                    new StartupBlock(default,
                        new ShowTaskStatement(default,
                            new UnaryOperatorExpression(default,
                                Operator.Not,
                                new NumberLiteralExpression(default, new BitsValue(0, 3))
                            )
                        )
                    )
                }
            }, machine);

            machine.AssertPrinted("7");
        }
    }
}
