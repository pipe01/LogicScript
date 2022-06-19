using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using LogicScript.ByteCode.DevEx;

namespace LogicScript.ByteCode
{
    internal partial class TapeReader
    {
        public readonly byte[] Data;
        public int Position;

        public bool IsEOF => Position >= Data.Length;

        public TapeReader(byte[] data)
        {
            this.Data = data;
            this.Position = 0;
        }

        public TapeReader Clone() => new TapeReader(Data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void JumpToAddress() => Position = ReadAddress();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            if (Position >= Data.Length)
                Position = 0;

            return Data[Position++];
        }

        public Header ReadHeader()
        {
            Header header = new();
            header.Read(Data[Position..]);

            Position += Header.Size;
            return header;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OpCodes ReadOpCode() => (OpCodes)ReadByte();

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadAddress() => (int)ReadUInt32();
    }
}