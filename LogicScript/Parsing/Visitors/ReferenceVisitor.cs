using Antlr4.Runtime.Misc;
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

        public override Reference VisitRefPort([NotNull] LogicScriptParser.RefPortContext context)
        {
            var identName = context.IDENT().GetText();
            PortInfo port;

            MachinePorts? target =
                          Context.Outer.Script.Inputs.TryGetValue(identName, out port) ? MachinePorts.Input
                        : Context.Outer.Script.Outputs.TryGetValue(identName, out port) ? MachinePorts.Output
                        : Context.Outer.Script.Registers.TryGetValue(identName, out port) ? MachinePorts.Register
                        : null;

            if (target == null)
                Context.Errors.AddError($"Unknown identifier '{identName}'", new SourceSpan(context.IDENT().Symbol), isFatal: true);

            return new PortReference(context.Span(), port);
        }

        public override Reference VisitRefLocal([NotNull] LogicScriptParser.RefLocalContext context)
        {
            var name = context.VARIABLE().GetText().TrimStart('$');

            if (!Context.Locals.TryGetValue(name, out var local))
            {
                Context.Errors.AddError($"Local variable ${name} is not declared", context.Span());
                return new LocalReference(context.Span(), name, default);
            }

            return new LocalReference(context.Span(), name, local);
        }
    }
}
