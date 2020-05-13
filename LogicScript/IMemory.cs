using LogicScript.Data;
using System;

namespace LogicScript
{
    public interface IMemory
    {
        int Capacity { get; }

        void Read(int start, Span<bool> inputs);
        void Write(int start, Span<bool> values);
    }

    public sealed class VolatileMemory : IMemory
    {
        public int Capacity { get; }

        private readonly bool[] Memory;

        public VolatileMemory() : this(256)
        {
        }

        public VolatileMemory(int capacity)
        {
            this.Memory = new bool[capacity];
            this.Capacity = capacity;
        }

        public void Clear()
        {
            for (int i = 0; i < Memory.Length; i++)
            {
                Memory[i] = false;
            }
        }

        public void Read(int start, Span<bool> inputs)
        {
            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i] = this.Memory[i + start];
            }
        }

        public void Write(int start, Span<bool> values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                this.Memory[i + start] = values[i];
            }
        }
    }
}
