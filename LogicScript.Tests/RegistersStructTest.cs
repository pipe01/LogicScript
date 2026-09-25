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
            Assert.AreEqual(1 + (2 * 4) + 8, regs.RegistersSize);
        }

        [Test]
        public void TestOneSmallRegister()
        {
            DecodeEncode([Port(5, 1, 0)], [123]);
        }

        [Test]
        public void TestTwoSmallRegisters()
        {
            DecodeEncode([Port(5, 1, 0), Port(3, 1, 1)], [123, 0b10101010]);
        }

        [Test]
        public void TestVectors()
        {
            DecodeEncode([Port(5, 3, 0), Port(16, 2, 1)], [123, 69, 42, 0, 1, 42, 0]);
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
    }
}
