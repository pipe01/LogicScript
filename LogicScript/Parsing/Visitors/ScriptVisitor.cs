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
            var ctx = new VisitContext(script);

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
                else if (decl.const_decl() != null)
                {
                    var value = new ExpressionVisitor(ctx).Visit(decl.const_decl().expression());

                    if (!value.IsConstant)
                        throw new ParseException("Const declarations must have a constant value", decl.const_decl().expression().Loc());

                    ctx.Constants.Add(decl.const_decl().IDENT().GetText(), value);
                }
                else if (decl.when_decl() != null)
                {
                    var body = new StatementVisitor(ctx).Visit(decl.when_decl().block());

                    script.Blocks.Add(new WhenBlock(decl.Loc(), body));
                }
            }

            return script;

            static PortInfo GetPortInfo(ITerminalNode bitSize, int index)
            {
                return new PortInfo(index, bitSize == null ? 1 : int.Parse(bitSize.GetText().TrimStart('\'')));
            }
        }
    }
}
