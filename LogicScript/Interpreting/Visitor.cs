using LogicScript.Parsing.Structures;
using System;

namespace LogicScript.Interpreting
{
    internal readonly ref partial struct Visitor
    {
        private readonly IMachine Machine;
        private readonly Span<bool> Input;

        public Visitor(IMachine machine, Span<bool> input)
        {
            this.Machine = machine;
            this.Input = input;
        }

        public void Visit(WhenBlock block)
        {
            if (block.Condition != null && Visit(block.Condition).Number == 0)
                return;

            Visit(block.Body);
        }
    }
}
