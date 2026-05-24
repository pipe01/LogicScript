using System;
using System.Collections.Generic;
using LogicScript.Data;

namespace LogicScript.Testing
{
    internal class TestingMachine(int inputCount, int outputCount) : IMachine
    {
        public int InputCount { get; } = inputCount;
        public int OutputCount { get; } = outputCount;

        public bool[] Inputs { get; } = new bool[inputCount];
        public bool[] Outputs { get; } = new bool[outputCount];

        private ulong[] Memory = [];
        public readonly IList<string> PrintOutput = [];

        public void Reset()
        {
            Array.Clear(Inputs, 0, Inputs.Length);
            Array.Clear(Outputs, 0, Outputs.Length);
            Memory = [];
            PrintOutput.Clear();
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
            PrintOutput.Add(msg);
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

        public ulong ReadRegister(int index)
        {
            return Memory[index];
        }

        public void WriteRegister(int index, ulong value)
        {
            Memory[index] = value;
        }
    }
}