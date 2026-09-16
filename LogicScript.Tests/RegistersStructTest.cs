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
            DecodeEncode([new(5, 1)], [123]);
        }

        [Test]
        public void TestTwoSmallRegisters()
        {
            DecodeEncode([new(5, 1), new(3, 1)], [123, 0b10101010]);
        }
    }
}