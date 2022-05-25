using System.Buffers.Binary;

namespace LogicScript.ByteCode
{
    internal class TapeReader
    {
        public readonly byte[] Data;
        public int Position;

        public bool IsEOF => Position >= Data.Length;

        public TapeReader(byte[] data)
        {
            this.Data = data;
            this.Position = 0;
        }

        public void JumpToAddress() => Position = ReadAddress();

        public byte ReadByte() => Data[Position++];

        public Header ReadHeader()
        {
            Header header = new();
            header.Read(Data[Position..]);

            Position += Header.Size;
            return header;
        }

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