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

            ReferenceTarget? target =
                          Context.Outer.Script.Inputs.TryGetValue(identName, out port) ? ReferenceTarget.Input
                        : Context.Outer.Script.Outputs.TryGetValue(identName, out port) ? ReferenceTarget.Output
                        : Context.Outer.Script.Registers.TryGetValue(identName, out port) ? ReferenceTarget.Register
                        : null;

            if (target == null)
            {
                Context.Errors.AddError($"Unknown identifier '{identName}'", new SourceLocation(context.IDENT().Symbol));
                return new PortReference(ReferenceTarget.Register, 0, 0);
            }

            return new PortReference(target.Value, port.StartIndex, port.BitSize);
        }

        public override IReference VisitRefLocal([NotNull] LogicScriptParser.RefLocalContext context)
        {
            var name = context.VARIABLE().GetText().TrimStart('$');

            if (!Context.Locals.TryGetValue(name, out var local))
            {
                Context.Errors.AddError($"Local variable ${name} is not declared", context.Loc());
                return new LocalReference(name, 0);
            }

            return new LocalReference(name, local.BitSize);
        }
    }
}
