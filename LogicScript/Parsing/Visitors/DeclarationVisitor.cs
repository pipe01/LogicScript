using Antlr4.Runtime.Misc;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using System.Collections.Generic;
using System.Linq;

namespace LogicScript.Parsing.Visitors
{
    internal class DeclarationVisitor : LogicScriptBaseVisitor<object?>
    {
        private readonly ScriptContext Context;
        private readonly ErrorSink Errors;

        private Script Script => Context.Script;

        public DeclarationVisitor(ScriptContext context, ErrorSink errors)
        {
            this.Context = context;
            this.Errors = errors;
        }

        public override object? VisitDecl_input([NotNull] LogicScriptParser.Decl_inputContext context)
        {
            Visit(context.port_info(), Script.Inputs, MachinePorts.Input);
            return null;
        }

        public override object? VisitDecl_output([NotNull] LogicScriptParser.Decl_outputContext context)
        {
            Visit(context.port_info(), Script.Outputs, MachinePorts.Output);
            return null;
        }

        public override object? VisitDecl_register([NotNull] LogicScriptParser.Decl_registerContext context)
        {
            Visit(context.port_info(), Script.Registers, MachinePorts.Register);
            return null;
        }

        public override object? VisitDecl_const([NotNull] LogicScriptParser.Decl_constContext context)
        {
            var value = new ExpressionVisitor(new BlockContext(Context, true)).Visit(context.expression());

            if (!value.IsConstant)
                Errors.AddError("Const declarations must have a constant value", value);

            Context.Constants.Add(context.IDENT().GetText(), value);
            return null;
        }

        public override object? VisitDecl_when([NotNull] LogicScriptParser.Decl_whenContext context)
        {
            var blockCtx = new BlockContext(Context, false);
            Expression? cond;

            if (context.any != null)
            {
                cond = null;
            }
            else if (context.cond != null)
            {
                cond = new ExpressionVisitor(blockCtx).Visit(context.cond);
            }
            else
            {
                Context.Errors.AddError("Missing 'when' condition", context.Span());
                return null;
            }

            var body = new StatementVisitor(blockCtx).Visit(context.block());

            Script.Blocks.Add(new WhenBlock(context.Span(), cond, body));
            return null;
        }

        public override object? VisitDecl_assign([NotNull] LogicScriptParser.Decl_assignContext context)
        {
            var body = new StatementVisitor(new BlockContext(Context, false)).Visit(context.stmt_assign());

            if (body is AssignStatement assign)
                Script.Blocks.Add(new AssignBlock(context.Span(), assign));
            else
                Errors.AddError("Assignment block must contain an assignment", body);

            return null;
        }

        public override object? VisitDecl_startup([NotNull] LogicScriptParser.Decl_startupContext context)
        {
            var blockCtx = new BlockContext(Context, false);
            var body = new StatementVisitor(blockCtx).Visit(context.block());

            Script.Blocks.Add(new StartupBlock(context.Span(), body));
            return null;
        }

        private void Visit(LogicScriptParser.Port_infoContext context, IDictionary<string, PortInfo> dic, MachinePorts target)
        {
            var size = context.size == null ? 1 : context.size.GetConstantValue(Context);

            if (size > BitsValue.BitSize)
                Errors.AddError($"The maximum bit size is {BitsValue.BitSize}", context.Span());

            var name = context.IDENT().GetText();

            if (Script.Inputs.ContainsKey(name) || Script.Outputs.ContainsKey(name) || Script.Registers.ContainsKey(name))
            {
                Errors.AddError($"The port '{name}' is already registered", new SourceSpan(context.IDENT().Symbol));
                return;
            }

            int startIndex;
            if (target == MachinePorts.Register)
                startIndex = Script.Registers.Count;
            else
                startIndex = dic.Values.Sum(o => o.BitSize);

            dic.Add(name, new PortInfo(target, startIndex, (int)size, new(context.IDENT().Symbol)));
        }
    }
}
