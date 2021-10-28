using LogicScript.Parsing.Structures;
using System;

namespace LogicScript.Interpreting
{
    internal readonly ref partial struct Visitor
    {
        private readonly Script Script;
        private readonly IMachine Machine;
        private readonly Span<bool> Input;

        public Visitor(Script script, IMachine machine, Span<bool> input)
        {
            this.Machine = machine;
            this.Input = input;
            this.Script = script;
        }

        public void Visit(WhenBlock block)
        {
            if (block.Condition != null && Visit(block.Condition).Number == 0)
                return;

            Visit(block.Body);
        }
    }
}
