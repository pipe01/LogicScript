using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogicScript.Data;

namespace LogicScript.Testing
{
    internal class TestingMachine(int inputCount, int outputCount) : IMachine
    {
        public int InputCount { get; } = inputCount;
        public int OutputCount { get; } = outputCount;

        public bool[] Inputs { get; } = new bool[inputCount];
        public bool[] Outputs { get; } = new bool[outputCount];

        private BitsValue[] Memory = Array.Empty<BitsValue>();
        private StringBuilder PrintOutput = new StringBuilder();

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

        public BitsValue ReadInputs()
        {
            return new BitsValue(Inputs);
        }

        public bool ReadInput(int index)
        {
            return Inputs[index];
        }

        public void WriteOutputs(int startIndex, BitsValue value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                var bitValue = (value.Number >> i) & 1UL;
                Outputs[startIndex + i] = bitValue == 1;
            }
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