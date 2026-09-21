using System;
using System.Collections.Generic;
using System.Linq;
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

        public DummyMachine(bool[]? inputs = null, int outputCount = 0)
        {
            this.InputCount = inputs?.Length ?? 0;
            this.Inputs = inputs ?? Array.Empty<bool>();
            this.OutputCount = outputCount;
        }

        public void Print(string msg)
        {
            Printed.Add(msg);
        }

        public bool ReadInput(int index)
        {
            return Inputs[index];
        }

        public void WriteOutputs(int startIndex, BitsValue value)
        {
        }

        public void WriteOutput(int index, bool value)
        {
        }

        public void AssertPrinted(params string[] lines)
        {
            Assert.AreEqual(lines.Select(o => o + "\n"), Printed);
        }

        public BitsValue ReadInputs(int startIndex, int count)
        {
            return new(Inputs.AsSpan()[startIndex..(startIndex + count)]);
        }
    }
}