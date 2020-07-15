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

    public sealed class BufferedMemory : IMemory
    {
        public int Capacity { get; private set; }
        public bool[] Buffer { get; private set; }

        public BufferedMemory() : this(256)
        {
        }

        public BufferedMemory(int capacity) : this(new bool[capacity])
        {
        }

        public BufferedMemory(bool[] buffer)
        {
            SetBuffer(buffer);
        }

        public void SetBuffer(bool[] buffer)
        {
            this.Buffer = buffer;
            this.Capacity = buffer.Length;
        }

        public void Clear()
        {
            for (int i = 0; i < Buffer.Length; i++)
            {
                Buffer[i] = false;
            }
        }

        public BitsValue Read(int start, int count)
        {
#if NETCOREAPP3_1
            return new BitsValue(Buffer[start..(start + count)]);
#else
            return new BitsValue(Buffer.AsSpan().Slice(start, count));
#endif
        }

        public void Write(int start, BitsValue value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                this.Buffer[i + start] = value[i];
            }
        }
    }
}
