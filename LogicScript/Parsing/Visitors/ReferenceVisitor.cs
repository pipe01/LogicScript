using Antlr4.Runtime.Misc;
using LogicScript.Parsing.Structures;
using System;

namespace LogicScript.Parsing.Visitors
{
    internal class ReferenceVisitor : LogicScriptBaseVisitor<Reference>
    {
        private readonly Script Script;

        public ReferenceVisitor(Script script)
        {
            this.Script = script;
        }

        public override Reference VisitReference([NotNull] LogicScriptParser.ReferenceContext context)
        {
            var identName = context.IDENT().GetText();

            var target = Script.Inputs.ContainsKey(identName) ? ReferenceTarget.Input
                        : Script.Outputs.ContainsKey(identName) ? ReferenceTarget.Output
                        : Script.Registers.ContainsKey(identName) ? ReferenceTarget.Register
                        : throw new Exception("Undefined identifier"); //TODO Throw proper exception type

            return new Reference(target, identName);
        }
    }
}
