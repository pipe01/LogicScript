using LogicScript.Data;
using LogicScript.Parsing.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LogicScript.Interpreting
{
    internal readonly ref partial struct Visitor
    {
        private readonly IMachine? Machine;
        private readonly Span<bool> Input;
        private readonly bool NotConstant;

        private readonly IDictionary<string, BitsValue> Locals;

        public Visitor(IMachine machine, Span<bool> input)
        {
            this.Machine = machine;
            this.Input = input;
            this.NotConstant = true;
            this.Locals = new Dictionary<string, BitsValue>();
        }

        [MemberNotNull(nameof(Machine), nameof(Locals))]
        private void AssertNotConstant(ICodeNode node, string message = "Not allowed in a constant scope")
        {
            if (!NotConstant)
                throw new NotConstantException(message, node);
        }
    }
}
