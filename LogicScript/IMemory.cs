using LogicScript.Data;
using System;

namespace LogicScript
{
    public interface IMemory
    {
        int Capacity { get; }

        void Read(BitRange range, Span<bool> inputs);
        void Write(BitRange range, Span<bool> values);
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

        public void Read(BitRange range, Span<bool> inputs)
        {
            for (int i = 0; i < range.Length; i++)
            {
                inputs[i] = this.Memory[i + range.Start];
            }
        }

        public void Write(BitRange range, Span<bool> values)
        {
            for (int i = 0; i < range.Length; i++)
            {
                this.Memory[i + range.Start] = values[i];
            }
        }
    }
}
