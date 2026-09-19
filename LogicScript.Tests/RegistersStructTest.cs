using System;
using LogicScript.Compiling;
using NUnit.Framework;

namespace LogicScript.Tests
{
    public class RegistersStructTest
    {
        private static IRegisters Compile(MachineRegister[] regs)
        {
            var regsType = RegistersStruct.Generate(regs);
            return (IRegisters)Activator.CreateInstance(regsType)!;
        }

        private static void DecodeEncode(MachineRegister[] machineRegs, byte[] data)
        {
            var regs = Compile(machineRegs);
            regs.Decode(data);

            Span<byte> newData = stackalloc byte[data.Length];
            regs.Encode(newData);

            Assert.AreEqual(data, newData.ToArray());
        }

        [Test]
        public void TestReset()
        {
            var regs = Compile([new(8, 1, 0), new(13, 4, 1), new(64, 1, 0)]);
            regs.SetRegister(0, 0, 10);
            regs.SetRegister(1, 0, 100);
            regs.SetRegister(1, 1, 101);
            regs.SetRegister(1, 2, 102);
            regs.SetRegister(1, 3, 103);
            regs.SetRegister(2, 0, 123123);

            Assert.AreEqual(10, regs.GetRegister(0, 0));
            Assert.AreEqual(100, regs.GetRegister(1, 0));
            Assert.AreEqual(101, regs.GetRegister(1, 1));
            Assert.AreEqual(102, regs.GetRegister(1, 2));
            Assert.AreEqual(103, regs.GetRegister(1, 3));
            Assert.AreEqual(123123, regs.GetRegister(2, 0));

            regs.Reset();

            Assert.AreEqual(0, regs.GetRegister(0, 0));
            Assert.AreEqual(0, regs.GetRegister(1, 0));
            Assert.AreEqual(0, regs.GetRegister(1, 1));
            Assert.AreEqual(0, regs.GetRegister(1, 2));
            Assert.AreEqual(0, regs.GetRegister(1, 3));
            Assert.AreEqual(0, regs.GetRegister(2, 0));
        }

        [Test]
        public void TestOutOfBoundsException()
        {
            var regs = Compile([new(8, 1, 0), new(13, 4, 1), new(64, 1, 0)]);

            Assert.Throws<IndexOutOfRangeException>(() => regs.GetRegister(3, 0));
            Assert.Throws<IndexOutOfRangeException>(() => regs.GetRegister(1, 4));
            Assert.Throws<IndexOutOfRangeException>(() => regs.SetRegister(3, 0, 123));
            Assert.Throws<IndexOutOfRangeException>(() => regs.SetRegister(1, 4, 123));
        }

        [Test]
        public void TestSize()
        {
            var regs = Compile([new(8, 1, 0), new(13, 4, 1), new(64, 1, 0)]);
            Assert.AreEqual(1 + (2 * 4) + 8, regs.Size);
        }

        [Test]
        public void TestOneSmallRegister()
        {
            DecodeEncode([new(5, 1, 0)], [123]);
        }

        [Test]
        public void TestTwoSmallRegisters()
        {
            DecodeEncode([new(5, 1, 0), new(3, 1, 1)], [123, 0b10101010]);
        }

        [Test]
        public void TestVectors()
        {
            DecodeEncode([new(5, 3, 0), new(16, 2, 1)], [123, 69, 42, 0, 1, 42, 0]);
        }

        [Test]
        public void TestGetRegisterSingle()
        {
            var regs = Compile([new(13, 1, 0)]);
            regs.SetRegister(0, 0, 123);

            Assert.AreEqual(123, regs.GetRegister(0, 0));
        }

        [Test]
        public void TestGetRegisterVector()
        {
            var regs = Compile([new(13, 4, 0)]);
            regs.SetRegister(0, 0, 100);
            regs.SetRegister(0, 1, 101);
            regs.SetRegister(0, 2, 102);
            regs.SetRegister(0, 3, 103);

            Assert.AreEqual(100, regs.GetRegister(0, 0));
            Assert.AreEqual(101, regs.GetRegister(0, 1));
            Assert.AreEqual(102, regs.GetRegister(0, 2));
            Assert.AreEqual(103, regs.GetRegister(0, 3));
        }
    }
}
