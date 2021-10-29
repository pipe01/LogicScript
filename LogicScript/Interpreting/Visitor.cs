using LogicScript.Data;
using LogicScript.Parsing.Structures.Blocks;
using System;
using System.Collections.Generic;

namespace LogicScript.Interpreting
{
    internal readonly ref partial struct Visitor
    {
        private readonly IMachine Machine;
        private readonly Span<bool> Input;

        private readonly IDictionary<string, BitsValue> Locals;

        public Visitor(IMachine machine, Span<bool> input)
        {
            this.Machine = machine;
            this.Input = input;
            this.Locals = new Dictionary<string, BitsValue>();
        }
    }
}
