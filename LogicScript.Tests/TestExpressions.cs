using System;
using LogicScript.Compiling;
using NUnit.Framework;

namespace LogicScript.Tests
{
    [TestFixture(Optimizations.All)]
    [TestFixture(Optimizations.None)]
    internal class TestExpressions(Optimizations Optimizations) : BaseTest
    {
        private void AssertExpression(string expr, ulong value)
        {
            Run($@"
            startup
                @print {expr}
            end
            ", out var machine, optimizations: Optimizations);

            Assert.AreEqual(machine.Printed.Count, 1);
            Assert.AreEqual(value.ToString(), machine.Printed[0].Trim());
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
        [TestCase(4, 2, "**", 16)]
        [TestCase(4, 3, "**", 64)]
        [TestCase(3, 8, "**", 6561)]
        [TestCase(5, 14, "**", 6103515625)]
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
        public void BinaryOperators(int a, int b, string op, object result)
        {
            AssertExpression($"{a} {op} {b}", Convert.ToUInt64(result));
        }

        [Test]
        [TestCase(0, 4, "!", 15)]
        [TestCase(1, 1, "!", 0)]
        [TestCase(3, 3, "!", 4)]
        [TestCase(1, 32, "!", 0xFFFFFFFE)]
        [TestCase(0, 3, "len", 3)]
        [TestCase(2, 3, "len", 3)]
        [TestCase(0, 3, "allOnes", 0)]
        [TestCase(1, 3, "allOnes", 0)]
        [TestCase(7, 3, "allOnes", 1)]
        public void UnaryOperators(int val, int len, string op, object result)
        {
            AssertExpression($"{op}(({val})'{len})", Convert.ToUInt64(result));
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
            ", machine, optimizations: Optimizations);

            machine.AssertPrinted("3");
        }

        [Test]
        public void AddRegisters()
        {
            Run(@"
            reg'4 a
            reg'4 b

            startup
                @print a + b
            end
            ", out var machine, [1, 0, 0, 0, 2, 0, 0, 0], optimizations: Optimizations);

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
            AssertExpression("1101b{<0,1}", 1);
            AssertExpression("1101b{<0,2}", 0b11);
            AssertExpression("1101b{<1,2}", 0b10);
            AssertExpression("1101b{<1,3}", 0b101);
            AssertExpression("1101b{<2,1}", 0);
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

        [Test]
        public void GetInputVector()
        {
            var machine = new DummyMachine([false, false, true, true, false, false]);

            Run(@"
            input'2 a[3]

            startup
                @print a[1]
            end
            ", machine, optimizations: Optimizations);

            machine.AssertPrinted("3");
        }

        [Test]
        public void PrintNegativeNumberIsNotNegative()
        {
            Run(@"
            startup
                @print 0xFFFFFFFFFFFFFFFF
            end
            ", out var machine, optimizations: Optimizations);

            machine.AssertPrinted("18446744073709551615");
        }
    }
}
