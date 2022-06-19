using System;

namespace LogicScript.ByteCode
{
    internal struct Header
    {
        public static int Size => sizeof(byte) * 2;

        public byte LocalsCount;
        public byte RegisterCount;

        public void Write(Span<byte> data)
        {
            data[0] = LocalsCount;
            data[1] = RegisterCount;
        }

        public void Read(ReadOnlySpan<byte> data)
        {
            LocalsCount = data[0];
            RegisterCount = data[1];
        }
    }
}