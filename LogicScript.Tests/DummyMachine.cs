using System;
using System.Collections.Generic;
using LogicScript.Data;
using NUnit.Framework;

namespace LogicScript.Tests
{
    public class DummyMachine : IMachine
    {
        public int InputCount { get; set; }
        public int OutputCount { get; set; }

        public bool[] Inputs { get; set; }

        public IList<string> Printed { get; set; } = new List<string>();

        private ulong[] Registers;

        public DummyMachine(bool[]? inputs = null, int outputCount = 0, ulong[]? registers = null)
        {
            this.InputCount = inputs?.Length ?? 0;
            this.Inputs = inputs ?? Array.Empty<bool>();
            this.Registers = registers ?? [];
            this.OutputCount = outputCount;
        }

        public void AllocateRegisters(int count)
        {
            Array.Resize(ref Registers, count);
        }

        public void Print(string msg)
        {
            Printed.Add(msg);
        }

        public bool ReadInput(int index)
        {
            return Inputs[index];
        }

        public BitsValue ReadInputs()
        {
            return new(Inputs);
        }

        public ulong ReadRegister(int index)
        {
            return Registers[index];
        }

        public void WriteOutputs(int startIndex, BitsValue value)
        {
        }

        public void WriteOutput(int index, bool value)
        {
        }

        public void WriteRegister(int index, ulong value)
        {
            Registers[index] = value;
        }

        public void AssertPrinted(params string[] lines)
        {
            Assert.AreEqual(lines, Printed);
        }
    }
}