using System;
using System.Collections.Generic;
using LogicScript.Data;
using LogicScript.Parsing.Structures;

namespace LogicScript.ByteCode
{
    internal ref partial struct Compiler
    {
        private const int InitialProgramCapacity = 100;
        private const int ProgramCapacityIncrement = 100;

        private byte[] Program;
        private int ProgramLength;
        private Header Header;

        private int CurrentPosition => ProgramLength - 1;
        private int NextPosition => ProgramLength;

        private readonly Script Script;
        private readonly IDictionary<string, byte> LocalsMap;
        private readonly Stack<Label> LoopStack;

        private Compiler(Script script)
        {
            this.Header = new();
            this.Script = script;
            this.Program = new byte[InitialProgramCapacity];
            this.LocalsMap = new Dictionary<string, byte>();
            this.LoopStack = new();

            this.ProgramLength = Header.Size;
        }

        private void Done()
        {
            Header.LocalsCount = (byte)LocalsMap.Count;
            Header.Write(Program);
        }

        public static byte[] Compile(Script script)
        {
            var compiler = new Compiler(script);

            foreach (var block in script.Blocks)
            {
                compiler.Visit(block);
            }

            compiler.Done();

            return compiler.Program[0..compiler.ProgramLength];
        }

        private void Push(byte num)
        {
            if (ProgramLength == Program.Length)
                Array.Resize(ref Program, Program.Length + ProgramCapacityIncrement);

            Program[ProgramLength] = num;
            ProgramLength++;
        }

        private void Push(OpCode op) => Push((byte)op);
        private void Push(ushort num)
        {
            Push((byte)(num >> 8));
            Push((byte)(num & 0xFF));
        }
        private void Push(short num) => Push((ushort)num);

        private void Push(uint num)
        {
            Push((byte)(num >> 24));
            Push((byte)((num >> 16) & 0xFF));
            Push((byte)((num >> 8) & 0xFF));
            Push((byte)(num & 0xFF));
        }
        private void Push(int num) => Push((uint)num);

        private void Push(ulong num)
        {
            Push((byte)(num >> 56));
            Push((byte)((num >> 48) & 0xFF));
            Push((byte)((num >> 40) & 0xFF));
            Push((byte)((num >> 32) & 0xFF));
            Push((byte)((num >> 24) & 0xFF));
            Push((byte)((num >> 16) & 0xFF));
            Push((byte)((num >> 8) & 0xFF));
            Push((byte)(num & 0xFF));
        }
        private void Push(long num) => Push((ulong)num);

        private void Push(BitsValue value)
        {
            Push(value.Number);
            Push((byte)value.Length);
        }

        private byte GetLocal(LocalInfo info)
        {
            if (!LocalsMap.TryGetValue(info.Name, out var idx))
            {
                LocalsMap[info.Name] = idx = (byte)LocalsMap.Count;
            }

            return idx;
        }
    }
}