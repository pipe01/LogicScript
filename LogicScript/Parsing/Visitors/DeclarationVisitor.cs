using Antlr4.Runtime.Misc;
using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
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
            Visit(context.port_info(), Script.Inputs);
            return null;
        }

        public override object? VisitDecl_output([NotNull] LogicScriptParser.Decl_outputContext context)
        {
            Visit(context.port_info(), Script.Outputs);
            return null;
        }

        public override object? VisitDecl_register([NotNull] LogicScriptParser.Decl_registerContext context)
        {
            Visit(context.port_info(), Script.Registers);
            return null;
        }

        public override object? VisitDecl_const([NotNull] LogicScriptParser.Decl_constContext context)
        {
            var value = new ExpressionVisitor(new BlockContext(Context, true)).Visit(context.expression());

            if (!value.IsConstant)
                Errors.AddError("Const declarations must have a constant value", context.expression().Loc());

            Context.Constants.Add(context.IDENT().GetText(), value);
            return null;
        }

        public override object? VisitDecl_when([NotNull] LogicScriptParser.Decl_whenContext context)
        {
            var blockCtx = new BlockContext(Context, false);
            var cond = context.cond == null ? null : new ExpressionVisitor(blockCtx).Visit(context.cond);
            var body = new StatementVisitor(blockCtx).Visit(context.block());

            Script.Blocks.Add(new WhenBlock(context.Loc(), cond, body));
            return null;
        }

        public override object? VisitDecl_assign([NotNull] LogicScriptParser.Decl_assignContext context)
        {
            var body = new StatementVisitor(new BlockContext(Context, false)).Visit(context.stmt_assign());

            if (body is AssignStatement assign)
                Script.Blocks.Add(new AssignBlock(context.Loc(), assign));
            else
                Errors.AddError("Assignment block must contain an assignment", context.stmt_assign().Loc());

            return null;
        }

        public override object? VisitDecl_startup([NotNull] LogicScriptParser.Decl_startupContext context)
        {
            var blockCtx = new BlockContext(Context, false);
            var body = new StatementVisitor(blockCtx).Visit(context.block());

            Script.Blocks.Add(new StartupBlock(context.Loc(), body));
            return null;
        }

        private void Visit(LogicScriptParser.Port_infoContext context, IDictionary<string, PortInfo> dic)
        {
            var size = context.BIT_SIZE() == null ? 1 : context.BIT_SIZE().ParseBitSize();

            if (size > BitsValue.BitSize)
                Errors.AddError($"The maximum bit size is {BitsValue.BitSize}", context.Loc());

            var name = context.IDENT().GetText();

            if (Script.Inputs.ContainsKey(name) || Script.Outputs.ContainsKey(name) || Script.Registers.ContainsKey(name))
            {
                Errors.AddError($"The port '{name}' is already registered", new SourceLocation(context.IDENT().Symbol));
                return;
            }

            int startIndex = dic.Values.Sum(o => o.BitSize);

            dic.Add(name, new PortInfo(startIndex, size));
        }
    }
}
