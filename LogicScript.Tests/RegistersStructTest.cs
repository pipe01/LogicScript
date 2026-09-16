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
