﻿using Antlr4.Runtime.Misc;
using LogicScript.Parsing.Structures;

namespace LogicScript.Parsing.Visitors
{
    internal class ReferenceVisitor : LogicScriptBaseVisitor<Reference>
    {
        private readonly BlockContext Context;

        public ReferenceVisitor(BlockContext context)
        {
            this.Context = context;
        }

        public override Reference VisitReference([NotNull] LogicScriptParser.ReferenceContext context)
        {
            var identName = context.IDENT().GetText();
            PortInfo port;

            var target = Context.Outer.Script.Inputs.TryGetValue(identName, out port) ? ReferenceTarget.Input
                        : Context.Outer.Script.Outputs.TryGetValue(identName, out port) ? ReferenceTarget.Output
                        : Context.Outer.Script.Registers.TryGetValue(identName, out port) ? ReferenceTarget.Register
                        : throw new ParseException($"Unknown identifier '{identName}'", new SourceLocation(context.IDENT().Symbol));

            return new Reference(target, port.StartIndex, port.BitSize);
        }
    }
}
