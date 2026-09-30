using System;
using System.Reflection;
using System.Reflection.Emit;
using LogicScript.Compiling;
using LogicScript.Parsing.Structures;
using NUnit.Framework;
using Sigil.NonGeneric;

namespace LogicScript.Tests
{
    public class RegistersStructTest
    {
        private static IRegistersInstance Compile(MachinePortInfo[] regs)
        {
            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("<>ScriptAssembly"), AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule("Module");
            var tb = mb.DefineType("Registers");
            tb.AddInterfaceImplementation(typeof(IRegistersInstance));

            var regsStruct = new RegistersStructBuilder(tb, regs);
            regsStruct.GenerateMethods();

            var ctorEmit = Emit.BuildConstructor(Type.EmptyTypes, tb, MethodAttributes.Public);
            regsStruct.EmitConstructorInit(ctorEmit);
            ctorEmit.LoadArgument(0);
            ctorEmit.Call(typeof(object).GetConstructor(Type.EmptyTypes));
            ctorEmit.Return();
            ctorEmit.CreateConstructor();

            return (IRegistersInstance)Activator.CreateInstance(tb.CreateType())!;
        }

        private static void DecodeEncode(MachinePortInfo[] machineRegs, byte[] data)
        {
            var regs = Compile(machineRegs);
            regs.DecodeRegisters(data);

            Span<byte> newData = stackalloc byte[data.Length];
            regs.EncodeRegisters(newData);

            Assert.AreEqual(data, newData.ToArray());
        }

        private static MachinePortInfo Port(int bitSize, int vectorLength, int index) => new("", MachinePorts.Placeholder, index, bitSize, vectorLength, null, default);

        [Test]
        public void TestReset()
        {
            var regs = Compile([Port(8, 1, 0), Port(13, 4, 1), Port(64, 1, 0)]);
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

            regs.ResetRegisters();

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
            var regs = Compile([Port(8, 1, 0), Port(13, 4, 1), Port(64, 1, 0)]);

            Assert.Throws<IndexOutOfRangeException>(() => regs.GetRegister(3, 0));
            Assert.Throws<IndexOutOfRangeException>(() => regs.GetRegister(1, 4));
            Assert.Throws<IndexOutOfRangeException>(() => regs.SetRegister(3, 0, 123));
            Assert.Throws<IndexOutOfRangeException>(() => regs.SetRegister(1, 4, 123));
        }

        [Test]
        public void TestSize()
        {
            var regs = Compile([Port(8, 1, 0), Port(13, 4, 1), Port(64, 1, 0)]);
            Assert.AreEqual(4 + (2 * 4) + 8, regs.RegistersSize);
        }

        [Test]
        public void TestOneSmallRegister()
        {
            DecodeEncode([Port(9, 1, 0)], [123, 0, 0, 0]);
            DecodeEncode([Port(15, 1, 0)], [123, 0, 0, 0]);
            DecodeEncode([Port(18, 1, 0)], [123, 0, 0, 0]);
            DecodeEncode([Port(32, 1, 0)], [123, 0, 0, 0]);
            DecodeEncode([Port(33, 1, 0)], [123, 0, 0, 0, 0, 0, 0, 0]);
            DecodeEncode([Port(63, 1, 0)], [123, 0, 0, 0, 0, 0, 0, 0]);
            DecodeEncode([Port(64, 1, 0)], [123, 0, 0, 0, 0, 0, 0, 0]);
        }

        [Test]
        public void TestTwoSmallRegisters()
        {
            DecodeEncode([Port(8, 1, 0), Port(8, 1, 1)], [123, 0, 0, 0, 0b10101010, 0, 0, 0]);
        }

        [Test]
        public void TestVectors()
        {
            DecodeEncode([Port(8, 3, 0), Port(16, 2, 1)], [123, 69, 42, 0, 1, 42, 0]);
        }

        [Test]
        public void TestGetRegisterSingle()
        {
            var regs = Compile([Port(13, 1, 0)]);
            regs.SetRegister(0, 0, 123);

            Assert.AreEqual(123, regs.GetRegister(0, 0));
        }

        [Test]
        public void TestGetRegisterVector()
        {
            var regs = Compile([Port(13, 4, 0)]);
            regs.SetRegister(0, 0, 100);
            regs.SetRegister(0, 1, 101);
            regs.SetRegister(0, 2, 102);
            regs.SetRegister(0, 3, 103);

            Assert.AreEqual(100, regs.GetRegister(0, 0));
            Assert.AreEqual(101, regs.GetRegister(0, 1));
            Assert.AreEqual(102, regs.GetRegister(0, 2));
            Assert.AreEqual(103, regs.GetRegister(0, 3));
        }

        [Test]
        public void TestDecodeBigNumberIntoSmallRegister()
        {
            var regs = Compile([Port(3, 1, 0)]);
            regs.DecodeRegisters([1, 2, 3, 4]);

            Assert.AreEqual(1, regs.GetRegister(0, 0));
        }

        [Test]
        public void TestDecodeBigNumberIntoSmallRegisterVector()
        {
            var regs = Compile([Port(3, 2, 0), Port(15, 2, 1), Port(25, 2, 2), Port(40, 2, 3)]);

            ushort n1 = (1 << 15) + 6;
            uint n2 = (1 << 25) + 8;
            ulong n3 = (1UL << 40) + 10;

            regs.DecodeRegisters([
                1,
                20,
                1, 0,
                (byte)(n1 & 0xFF), (byte)(n1 >> 8),
                1, 0, 0, 0,
                (byte)(n2 & 0xFF), (byte)((n2 >> 8) & 0xFF), (byte)(n2 >> 16), 0,
                1, 0, 0, 0, 0, 0, 0, 0,
                (byte)(n3 & 0xFF), (byte)((n3 >> 8) & 0xFF), (byte)((n3 >> 16) & 0xFF), (byte)((n3 >> 24) & 0xFF),
                    (byte)((n3 >> 32) & 0xFF), (byte)((n3 >> 40) & 0xFF), (byte)((n3 >> 48) & 0xFF), (byte)((n3 >> 56) & 0xFF)
            ]);

            Assert.AreEqual(1, regs.GetRegister(0, 0));
            Assert.AreEqual(4, regs.GetRegister(0, 1));
            Assert.AreEqual(1, regs.GetRegister(1, 0));
            Assert.AreEqual(6, regs.GetRegister(1, 1));
            Assert.AreEqual(1, regs.GetRegister(2, 0));
            Assert.AreEqual(8, regs.GetRegister(2, 1));
            Assert.AreEqual(1, regs.GetRegister(3, 0));
            Assert.AreEqual(10, regs.GetRegister(3, 1));
        }
    }
}
