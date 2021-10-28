using Antlr4.Runtime.Misc;
using LogicScript.Parsing.Structures;

namespace LogicScript.Parsing.Visitors
{
    internal class ReferenceVisitor : LogicScriptBaseVisitor<Reference>
    {
        private readonly VisitContext Context;

        public ReferenceVisitor(VisitContext context)
        {
            this.Context = context;
        }

        public override Reference VisitReference([NotNull] LogicScriptParser.ReferenceContext context)
        {
            var identName = context.IDENT().GetText();
            PortInfo port;

            var target = Context.Script.Inputs.TryGetValue(identName, out port) ? ReferenceTarget.Input
                        : Context.Script.Outputs.TryGetValue(identName, out port) ? ReferenceTarget.Output
                        : Context.Script.Registers.TryGetValue(identName, out port) ? ReferenceTarget.Register
                        : throw new ParseException($"Unknown identifier '{identName}'", new SourceLocation(context.IDENT().Symbol));

            return new Reference(target, port.Index, port.BitSize);
        }
    }
}
