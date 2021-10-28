using Antlr4.Runtime.Misc;
using LogicScript.Data;
using LogicScript.Parsing.Structures;
using System.Collections.Generic;

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
                    VisitPortInfo(decl.input_decl().port_info(), script.Inputs);
                }
                else if (decl.output_decl() != null)
                {
                    VisitPortInfo(decl.output_decl().port_info(), script.Outputs);
                }
                else if (decl.register_decl() != null)
                {
                    VisitPortInfo(decl.register_decl().port_info(), script.Registers);
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
                    var cond = decl.when_decl().cond == null ? null : new ExpressionVisitor(ctx).Visit(decl.when_decl().cond);
                    var body = new StatementVisitor(ctx).Visit(decl.when_decl().block());

                    script.Blocks.Add(new WhenBlock(decl.Loc(), cond, body));
                }
            }

            return script;

            void VisitPortInfo(LogicScriptParser.Port_infoContext context, IDictionary<string, PortInfo> dic)
            {
                var size = context.BIT_SIZE() == null ? 1 : int.Parse(context.BIT_SIZE().GetText().TrimStart('\''));

                if (size > BitsValue.BitSize)
                    throw new ParseException($"The maximum bit size is {BitsValue.BitSize}", context.Loc());

                var name = context.IDENT().GetText();

                if (script.Inputs.ContainsKey(name) || script.Outputs.ContainsKey(name) || script.Registers.ContainsKey(name))
                    throw new ParseException($"The port '{name}' is already registered", new SourceLocation(context.IDENT().Symbol));

                dic.Add(name, new PortInfo(dic.Count, size));
            }
        }
    }
}
