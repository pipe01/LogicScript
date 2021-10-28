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

            var target = Context.Script.Inputs.ContainsKey(identName) ? ReferenceTarget.Input
                        : Context.Script.Outputs.ContainsKey(identName) ? ReferenceTarget.Output
                        : Context.Script.Registers.ContainsKey(identName) ? ReferenceTarget.Register
                        : throw new ParseException($"Unknown identifier '{identName}'", new SourceLocation(context.IDENT().Symbol));

            return new Reference(target, identName);
        }
    }
}
