using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogicScript.Data;

namespace LogicScript.Testing
{
    internal class TestingMachine : IMachine, IUpdatableMachine
    {
        public int InputCount { get; }
        public int OutputCount { get; }

        public bool[] Inputs { get; }
        public bool[] Outputs { get; }

        private BitsValue[] Memory;
        private StringBuilder PrintOutput;

        public TestingMachine(int inputCount, int outputCount)
        {
            this.InputCount = inputCount;
            this.OutputCount = outputCount;
            this.Inputs = new bool[inputCount];
            this.Outputs = new bool[outputCount];
            this.Memory = Array.Empty<BitsValue>();
            this.PrintOutput = new StringBuilder();
        }

        public void AllocateRegisters(int count)
        {
            if (count > Memory.Length)
            {
                Array.Resize(ref Memory, count);
            }
        }

        public void Print(string msg)
        {
            PrintOutput.AppendLine(msg);
        }

        public void QueueUpdate()
        {
        }

        public void ReadInputs(Span<bool> values)
        {
            Inputs.CopyTo(values);
        }

        public bool ReadInput(int index)
        {
            return Inputs[index];
        }

        public void WriteOutputs(int startIndex, Span<bool> value)
        {
            value.CopyTo(Outputs.AsSpan()[startIndex..]);
        }

        public void WriteOutput(int index, bool value)
        {
            Outputs[index] = value;
        }

        public BitsValue ReadRegister(int index)
        {
            return Memory[index];
        }

        public void WriteRegister(int index, BitsValue value)
        {
            Memory[index] = value;
        }
    }
}