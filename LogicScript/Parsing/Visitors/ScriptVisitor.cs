using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using LogicScript.Parsing.Structures;

namespace LogicScript.Parsing.Visitors
{
    class ScriptVisitor : LogicScriptBaseVisitor<Script>
    {
        public override Script VisitScript([NotNull] LogicScriptParser.ScriptContext context)
        {
            var script = new Script();

            foreach (var decl in context.declaration())
            {
                if (decl.input_decl() != null)
                {
                    script.Inputs.Add(decl.input_decl().IDENT().GetText(), GetPortInfo(decl.input_decl().BIT_SIZE(), script.Inputs.Count));
                }
                else if (decl.output_decl() != null)
                {
                    script.Outputs.Add(decl.output_decl().IDENT().GetText(), GetPortInfo(decl.output_decl().BIT_SIZE(), script.Outputs.Count));
                }
                else if (decl.register_decl() != null)
                {
                    script.Registers.Add(decl.register_decl().IDENT().GetText(), GetPortInfo(decl.register_decl().BIT_SIZE(), script.Registers.Count));
                }
                else if (decl.when_decl() != null)
                {
                    var block = new StatementVisitor().Visit(decl.when_decl().block());
                }
            }

            return script;

            static PortInfo GetPortInfo(ITerminalNode bitSize, int index)
            {
                return new PortInfo(index, bitSize == null ? 1 : int.Parse(bitSize.GetText().TrimStart('[', ' ').TrimEnd(']', ' ')));
            }
        }
    }
}
