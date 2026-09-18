using System;

namespace LogicScript
{

    public interface IRegisters
    {
        /// <summary>
        /// Sum of the size in bytes of all registers.
        /// </summary>
        int Size { get; }

        void Reset();

        void Decode(ReadOnlySpan<byte> data);
        void Encode(Span<byte> data);

        ulong GetRegister(int index, int vector);
        void SetRegister(int index, int vector, ulong value);
    }

    public sealed class EmptyRegisters : IRegisters
    {
        public int Size => 0;

        public void Decode(ReadOnlySpan<byte> data)
        {
        }

        public void Encode(Span<byte> data)
        {
        }

        public ulong GetRegister(int index, int vector)
        {
            throw new IndexOutOfRangeException();
        }

        public void Reset()
        {
        }

        public void SetRegister(int index, int vector, ulong value)
        {
        }
    }
}