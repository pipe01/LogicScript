using System;
using Antlr4.Runtime.Misc;
using LogicScript.Parsing.Structures;

namespace LogicScript.Parsing.Visitors
{
    internal class ReferenceVisitor(BlockContext context, int defaultBitSize = 0, bool allowRawVectors = false) : LogicScriptParserBaseVisitor<Reference>
    {
        private readonly BlockContext Context = context;

        private MachinePortInfo GetPortInfo(string name, SourceSpan nameSpan)
        {
            MachinePorts? target =
                          Context.Script.Script.Inputs.TryGetValue(name, out var port) ? MachinePorts.Input
                        : Context.Script.Script.Outputs.TryGetValue(name, out port) ? MachinePorts.Output
                        : Context.Script.Script.Registers.TryGetValue(name, out port) ? MachinePorts.Register
                        : MachinePorts.Placeholder;

            if (target == MachinePorts.Placeholder)
            {
                Context.Errors.AddError($"Unknown port '{name}'", nameSpan);

                return new(MachinePorts.Placeholder, 0, defaultBitSize, 1, nameSpan);
            }

            return port;
        }

        public override Reference VisitRefPort([NotNull] LogicScriptParser.RefPortContext context)
        {
            var nameSpan = context.IDENT().Symbol.Span();
            var portInfo = GetPortInfo(context.IDENT().GetText(), nameSpan);

            if (portInfo.VectorLength > 1 && !allowRawVectors)
                Context.Errors.AddError("Vectored port must be indexed", context.Span());

            return new PortReference(context.Span(), nameSpan, portInfo, null);
        }

        public override Reference VisitRefLocal([NotNull] LogicScriptParser.RefLocalContext context)
        {
            var name = context.VARIABLE().GetText();

            if (!Context.TryGetLocal(name, out var local))
            {
                Context.Errors.AddError($"Local variable {name} is not declared", context.Span());
                return new LocalReference(context.Span(), name, default);
            }

            return new LocalReference(context.Span(), name, local);
        }

        public override Reference VisitRefIndex([NotNull] LogicScriptParser.RefIndexContext context)
        {
            var nameSpan = context.IDENT().Symbol.Span();
            var portInfo = GetPortInfo(context.IDENT().GetText(), nameSpan);

            int maxIndexerBitSize = (int)Math.Ceiling(Math.Log(portInfo.VectorLength, 2));
            var index = new ExpressionVisitor(Context, maxIndexerBitSize).Visit(context.simple_indexer().index);

            return new PortReference(context.Span(), nameSpan, portInfo, index);
        }
    }
}
