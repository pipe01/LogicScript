using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using Xunit;

namespace LogicScript.Tests
{
    public class RunBlocks
    {
        [Theory, ClassData(typeof(Runners))]
        public void Print_Startup(IRunner runner)
        {
            var machine = new DummyMachine();

            runner.Run(new Script
            {
                Blocks = {
                    new StartupBlock(default, new PrintTaskStatement(default, "nice1")),
                    new WhenBlock(default, null, new PrintTaskStatement(default, "nice2")),
                    new WhenBlock(default, new NumberLiteralExpression(default, 1), new PrintTaskStatement(default, "nice3")),
                }
            }, machine);

            machine.AssertPrinted("nice1", "nice2", "nice3");
        }

        [Theory, ClassData(typeof(Runners))]
        public void Print_AddLiterals(IRunner runner)
        {
            var machine = new DummyMachine();

            runner.Run(new Script
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

        [Theory, ClassData(typeof(Runners))]
        public void Print_AddInputs(IRunner runner)
        {
            var machine = new DummyMachine(new[] { true, true, false });

            var a = new PortInfo(MachinePorts.Input, 0, 1, default);
            var b = new PortInfo(MachinePorts.Input, 1, 2, default);

            runner.Run(new Script
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

        [Theory, ClassData(typeof(Runners))]
        public void Print_LiteralSliceRight(IRunner runner)
        {
            var machine = new DummyMachine();

            runner.Run(new Script
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

        [Theory, ClassData(typeof(Runners))]
        public void Print_LiteralSliceLeft(IRunner runner)
        {
            var machine = new DummyMachine();

            runner.Run(new Script
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

        [Theory, ClassData(typeof(Runners))]
        public void Print_TernaryTrue(IRunner runner)
        {
            var machine = new DummyMachine();

            runner.Run(new Script
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

        [Theory, ClassData(typeof(Runners))]
        public void Print_TernaryFalse(IRunner runner)
        {
            var machine = new DummyMachine();

            runner.Run(new Script
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

        [Theory, ClassData(typeof(Runners))]
        public void Print_Invert(IRunner runner)
        {
            var machine = new DummyMachine();

            runner.Run(new Script
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
