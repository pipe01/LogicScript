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

        private BitsValue[] Registers = Array.Empty<BitsValue>();

        public DummyMachine(bool[]? inputs = null, int outputCount = 0)
        {
            this.InputCount = inputs?.Length ?? 0;
            this.Inputs = inputs ?? Array.Empty<bool>();
            this.OutputCount = outputCount;
        }

        public void AllocateRegisters(int count)
        {
            Registers = new BitsValue[count];
        }

        public void Print(string msg)
        {
            Printed.Add(msg);
        }

        public void ReadInput(Span<bool> values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = Inputs[i];
            }
        }

        public BitsValue ReadRegister(int index)
        {
            return Registers[index];
        }

        public void WriteOutput(int startIndex, Span<bool> value)
        {
        }

        public void WriteRegister(int index, BitsValue value)
        {
            Registers[index] = value;
        }

        public void AssertPrinted(params string[] lines)
        {
            Assert.AreEqual(lines, Printed);
        }
    }
}