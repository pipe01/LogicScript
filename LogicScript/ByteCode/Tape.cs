using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Reflection;
using LogicScript.ByteCode.DevEx;

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

        public TapeReader Clone() => new TapeReader(Data);

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

        public void Dump(TextWriter w)
        {
            var tape = Clone();
            tape.ReadHeader();

            int stack = 0;

            while (!tape.IsEOF)
            {
                int opcodePos = tape.Position;
                
                var opcode = tape.ReadOpCode();

                var field = typeof(OpCode).GetField(opcode.ToString());
                var opAttr = field?.GetCustomAttribute<OpCodeAttribute>();
                var stackAttr = field?.GetCustomAttribute<StackAttribute>();

                if (opAttr == null)
                    throw new Exception("Invalid opcode value");

                var stackValue = stackAttr?.Amounts.Sum() ?? 0;
                stack += stackValue;

                w.Write(opcodePos.ToString().PadLeft(4));
                w.Write(' ');
                w.Write(opAttr.ShortName);
                w.Write(' ');

                bool isFirst = true;
                foreach (var arg in opAttr.Arguments)
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        w.Write(' ');

                    w.Write(arg.Name);
                    w.Write('=');

                    var value = arg.Bytes switch
                    {
                        1 => tape.ReadByte(),
                        2 => tape.ReadUInt16(),
                        4 => tape.ReadUInt32(),
                        8 => tape.ReadUInt64(),
                        _ => throw new Exception("Invalid opcode argument length")
                    };

                    w.Write(value);
                }

                w.WriteLine();
            }
        }
    }
}