using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using NUnit.Framework;

namespace LogicScript.Tests
{
    internal class TestExpressions : BaseTest
    {
        public TestExpressions(IRunner runner) : base(runner)
        {
        }

        [Test]
        [TestCase(5, 3, Operator.And, 1)]
        [TestCase(5, 3, Operator.Or, 7)]
        [TestCase(5, 3, Operator.Xor, 6)]
        [TestCase(3, 2, Operator.ShiftLeft, 12)]
        [TestCase(12, 2, Operator.ShiftRight, 3)]
        [TestCase(5, 3, Operator.Add, 8)]
        [TestCase(5, 3, Operator.Subtract, 2)]
        [TestCase(5, 3, Operator.Multiply, 15)]
        [TestCase(6, 3, Operator.Divide, 2)]
        [TestCase(5, 3, Operator.Divide, 1)]
        [TestCase(5, 3, Operator.Power, 125)]
        [TestCase(5, 3, Operator.Modulus, 2)]
        [TestCase(5, 3, Operator.EqualsCompare, 0)]
        [TestCase(5, 5, Operator.EqualsCompare, 1)]
        [TestCase(5, 3, Operator.NotEqualsCompare, 1)]
        [TestCase(5, 5, Operator.NotEqualsCompare, 0)]
        [TestCase(5, 3, Operator.Greater, 1)]
        [TestCase(3, 5, Operator.Greater, 0)]
        [TestCase(3, 3, Operator.Greater, 0)]
        [TestCase(5, 3, Operator.Lesser, 0)]
        [TestCase(3, 5, Operator.Lesser, 1)]
        [TestCase(3, 3, Operator.Lesser, 0)]
        public void BinaryOperators(int a, int b, Operator op, int result)
        {
            var machine = new DummyMachine();

            Runner.Run(new Script
            {
                Blocks = {
                    new StartupBlock(default,
                        new ShowTaskStatement(default,
                            new BinaryOperatorExpression(default, op,
                                new NumberLiteralExpression(default, a),
                                new NumberLiteralExpression(default, b)
                            )
                        )
                    ),
                }
            }, machine);

            machine.AssertPrinted(result.ToString());
        }

        [Test]
        [TestCase(3, 3, Operator.Not, 4)]
        [TestCase(0, 3, Operator.Length, 3)]
        [TestCase(2, 3, Operator.Length, 3)]
        [TestCase(0, 3, Operator.AllOnes, 0)]
        [TestCase(1, 3, Operator.AllOnes, 0)]
        [TestCase(7, 3, Operator.AllOnes, 1)]
        public void UnaryOperators(int val, int len, Operator op, int result)
        {
            var machine = new DummyMachine();

            Runner.Run(new Script
            {
                Blocks = {
                    new StartupBlock(default,
                        new ShowTaskStatement(default,
                            new UnaryOperatorExpression(default, op,
                                new NumberLiteralExpression(default, new((ulong)val, len))
                            )
                        )
                    ),
                }
            }, machine);

            machine.AssertPrinted(result.ToString());
        }

        [Test]
        public void AddInputs()
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
        public void LiteralSliceRight()
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
        public void LiteralSliceLeft()
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
        public void TernaryTrue()
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
        public void TernaryFalse()
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
        public void InvertNumber()
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
