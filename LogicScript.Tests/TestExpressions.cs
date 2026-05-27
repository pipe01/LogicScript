using LogicScript.Parsing.Structures;
using NUnit.Framework;

namespace LogicScript.Tests
{
    internal class TestExpressions(RunnerType runnerType) : BaseTest(runnerType)
    {
        private void AssertExpression(string expr, ulong value)
        {
            Run($@"
            startup
                @print {expr}
            end
            ", out var machine);

            Assert.AreEqual(machine.Printed.Count, 1);

            var printed = ulong.Parse(machine.Printed[0]);

            Assert.AreEqual(value, printed);
        }

        [Test]
        [TestCase(5, 3, "&", 1)]
        [TestCase(5, 3, "|", 7)]
        [TestCase(5, 3, "^", 6)]
        [TestCase(3, 2, "<<", 12)]
        [TestCase(12, 2, ">>", 3)]
        [TestCase(5, 3, "+", 8)]
        [TestCase(5, 3, "-", 2)]
        [TestCase(5, 3, "*", 15)]
        [TestCase(6, 3, "/", 2)]
        [TestCase(5, 3, "/", 1)]
        [TestCase(5, 3, "**", 125)]
        [TestCase(5, 3, "%", 2)]
        [TestCase(5, 3, "==", 0)]
        [TestCase(5, 5, "==", 1)]
        [TestCase(5, 3, "!=", 1)]
        [TestCase(5, 5, "!=", 0)]
        [TestCase(5, 3, ">", 1)]
        [TestCase(3, 5, ">", 0)]
        [TestCase(3, 3, ">", 0)]
        [TestCase(5, 3, "<", 0)]
        [TestCase(3, 5, "<", 1)]
        [TestCase(3, 3, "<", 0)]
        public void BinaryOperators(int a, int b, string op, int result)
        {
            AssertExpression($"{a} {op} {b}", (ulong)result);
        }

        [Test]
        [TestCase(3, 3, "!", 4)]
        [TestCase(0, 3, "len", 3)]
        [TestCase(2, 3, "len", 3)]
        [TestCase(0, 3, "allOnes", 0)]
        [TestCase(1, 3, "allOnes", 0)]
        [TestCase(7, 3, "allOnes", 1)]
        public void UnaryOperators(int val, int len, string op, int result)
        {
            AssertExpression($"{op}(({val})'{len})", (ulong)result);
        }

        [Test]
        public void AddInputs()
        {
            var machine = new DummyMachine([false, true, true]);

            Run(@"
            input a
            input'2 b

            startup
                @print a + b
            end
            ", machine);

            machine.AssertPrinted("3");
        }

        [Test]
        public void AddRegisters()
        {
            var machine = new DummyMachine(registers: [1, 2]);

            var a = new MachinePortInfo(MachinePorts.Register, 0, 1, 1, default);
            var b = new MachinePortInfo(MachinePorts.Register, 1, 1, 1, default);

            Run(@"
            reg a
            reg b

            startup
                @print a + b
            end
            ", machine);

            machine.AssertPrinted("3");
        }

        [Test]
        public void LiteralSliceRight()
        {
            AssertExpression("13{0,1}", 1);
            AssertExpression("13{1,2}", 2);
            AssertExpression("13{2,1}", 1);
        }

        [Test]
        public void LiteralSliceLeft()
        {
            AssertExpression("13{<1,2}", 2);
            AssertExpression("13{<2,1}", 1);
            AssertExpression("13{<0,1}", 1);
        }

        [Test]
        public void TernaryTrue()
        {
            AssertExpression("1 ? 10 : 20", 10);
        }

        [Test]
        public void TernaryFalse()
        {
            AssertExpression("0 ? 10 : 20", 20);
        }

        [Test]
        public void InvertNumber()
        {
            AssertExpression("~(0)'3", 7);
        }
    }
}
