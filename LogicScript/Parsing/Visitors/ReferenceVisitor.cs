using Antlr4.Runtime.Misc;
using LogicScript.Parsing.Structures;

namespace LogicScript.Parsing.Visitors
{
    internal class ReferenceVisitor : LogicScriptBaseVisitor<IReference>
    {
        private readonly BlockContext Context;

        public ReferenceVisitor(BlockContext context)
        {
            this.Context = context;
        }

        public override IReference VisitRefPort([NotNull] LogicScriptParser.RefPortContext context)
        {
            var identName = context.IDENT().GetText();
            PortInfo port;

            var target = Context.Outer.Script.Inputs.TryGetValue(identName, out port) ? ReferenceTarget.Input
                        : Context.Outer.Script.Outputs.TryGetValue(identName, out port) ? ReferenceTarget.Output
                        : Context.Outer.Script.Registers.TryGetValue(identName, out port) ? ReferenceTarget.Register
                        : throw new ParseException($"Unknown identifier '{identName}'", new SourceLocation(context.IDENT().Symbol));

            return new PortReference(target, port.StartIndex, port.BitSize);
        }

        public override IReference VisitRefLocal([NotNull] LogicScriptParser.RefLocalContext context)
        {
            var name = context.VARIABLE().GetText().TrimStart('$');

            if (!Context.Locals.TryGetValue(name, out var local))
                throw new ParseException($"Local variable ${name} is not declared", context.Loc());

            return new LocalReference(name, local.BitSize);
        }
    }
}
