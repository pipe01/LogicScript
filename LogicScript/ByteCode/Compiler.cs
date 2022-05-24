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
        private readonly IList<ushort> Program;

        private int CurrentPosition => Program.Count - 1;
        private int NextPosition => Program.Count;

        private Compiler(Script script)
        {
            this.Script = script;
            this.Program = new List<ushort>();
        }

        public static ushort[] Compile(Script script)
        {
            var compiler = new Compiler(script);
            
            var expr = ((script.Blocks[0] as StartupBlock)?.Body as BlockStatement)?.Statements[0];
            compiler.Visit(expr ?? throw new Exception("aaa"));

            return compiler.Program.ToArray();
        }

        private void Push(OpCode op) => Program.Add((ushort)op);

        private void Push(ushort num) => Program.Add(num);
        private void Push(short num) => Program.Add((ushort)num);

        private void Push(uint num)
        {
            Push((ushort)(num >> 16));
            Push((ushort)(num & 0xFFFF));
        }
        private void Push(int num)
        {
            Push((ushort)(num >> 16));
            Push((ushort)(num & 0xFFFF));
        }

        private void Push(ulong num)
        {
            Push((ushort)(num >> 48));
            Push((ushort)((num >> 32) & 0xFFFF));
            Push((ushort)((num >> 16) & 0xFFFF));
            Push((ushort)(num & 0xFFFF));
        }
        private void Push(long num)
        {
            Push((ushort)(num >> 48));
            Push((ushort)((num >> 32) & 0xFFFF));
            Push((ushort)((num >> 16) & 0xFFFF));
            Push((ushort)(num & 0xFFFF));
        }

        private void Push(BitsValue value)
        {
            Push(value.Number);
            Push((ushort)value.Length);
        }
    }
}