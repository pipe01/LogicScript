using System;
using System.Buffers.Binary;

namespace LogicScript.ByteCode
{
    public ref struct TapeReader
    {
        public readonly ReadOnlySpan<byte> Data;
        public int Position;

        public bool IsEOF => Position >= Data.Length;

        public TapeReader(ReadOnlySpan<byte> data)
        {
            this.Data = data;
            this.Position = 0;
        }

        public void JumpToAddress() => Position = ReadAddress();

        public byte ReadByte() => Data[Position++];

        public OpCode ReadOpCode() => (OpCode)ReadByte();

        public ushort ReadUInt16()
        {
            ushort value = BinaryPrimitives.ReadUInt16BigEndian(Data[Position..(Position + sizeof(ushort))]);
            Position += sizeof(ushort);
            return value;
        }
        public uint ReadUInt32()
        {
            uint value = BinaryPrimitives.ReadUInt32BigEndian(Data[Position..(Position + sizeof(uint))]);
            Position += sizeof(uint);
            return value;
        }
        public ulong ReadUInt64()
        {
            ulong value = BinaryPrimitives.ReadUInt64BigEndian(Data[Position..(Position + sizeof(ulong))]);
            Position += sizeof(ulong);
            return value;
        }

        public int ReadAddress() => (int)ReadUInt32();
    }
}