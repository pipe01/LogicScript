using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogicScript.Data;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Statements;

namespace LogicScript.ByteCode
{
    internal readonly ref partial struct Compiler
    {
        private readonly Script Script;
        private readonly IList<byte> Program;

        private int CurrentPosition => Program.Count - 1;
        private int NextPosition => Program.Count;

        private Compiler(Script script)
        {
            this.Script = script;
            this.Program = new List<byte>();
        }

        public static byte[] Compile(Script script)
        {
            var compiler = new Compiler(script);
            
            var expr = ((script.Blocks[0] as StartupBlock)?.Body as BlockStatement)?.Statements[0];
            compiler.Visit(expr ?? throw new Exception("aaa"));

            return compiler.Program.ToArray();
        }

        private void Push(byte num) => Program.Add(num);

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
    }
}