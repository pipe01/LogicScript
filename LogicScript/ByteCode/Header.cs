using System;

namespace LogicScript.ByteCode
{
    internal struct Header
    {
        public static int Size => sizeof(byte);

        public byte LocalsCount;

        public void Write(Span<byte> data)
        {
            data[0] = LocalsCount;
        }

        public void Read(ReadOnlySpan<byte> data)
        {
            LocalsCount = data[0];
        }
    }
}