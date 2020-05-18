using LogicScript.Data;
using System;

namespace LogicScript
{
    public interface IMemory
    {
        int Capacity { get; }

        BitsValue Read(int start, int count);
        void Write(int start, BitsValue value);
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

        public BitsValue Read(int start, int count)
        {
#if NETCOREAPP3_1
            return new BitsValue(Memory[start..(start + count)]);
#else
            return new BitsValue(Memory.AsSpan().Slice(start, count));
#endif
        }

        public void Write(int start, BitsValue value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                this.Memory[i + start] = value[i];
            }
        }
    }
}
