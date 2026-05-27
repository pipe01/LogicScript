using Antlr4.Runtime.Misc;
using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using System.Collections.Generic;
using System.Linq;

namespace LogicScript.Parsing.Visitors
{
    internal class DeclarationVisitor(ScriptContext context, ErrorSink errors) : LogicScriptBaseVisitor<object?>
    {
        private readonly ScriptContext Context = context;
        private readonly ErrorSink Errors = errors;

        private Script Script => Context.Script;

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
            var value = new ExpressionVisitor(new BlockContext(Context, null, true)).Visit(context.expression());

            if (!value.IsConstant)
                Errors.AddError("Const declarations must have a constant value", value);

            Context.Script.Constants.Add(context.IDENT().GetText(), value.GetConstantValue());
            return null;
        }

        public override object? VisitDecl_when([NotNull] LogicScriptParser.Decl_whenContext context)
        {
            Expression? cond;

            if (context.any != null)
            {
                cond = null;
            }
            else if (context.cond != null)
            {
                cond = new ExpressionVisitor(new BlockContext(Context)).Visit(context.cond);
            }
            else
            {
                Context.Errors.AddError("Missing 'when' condition", context.Span());

                cond = new PlaceholderExpression(new(context.space.Span().Start, context.space.Span().End));
            }

            var body = context.block() == null
                ? new BlockStatement(context.Span(), [], [])
                : new StatementVisitor(Context).Visit(context.block());

            Script.Blocks.Add(new WhenBlock(context.Span(), cond, body));
            return null;
        }

        public override object? VisitDecl_assign([NotNull] LogicScriptParser.Decl_assignContext context)
        {
            var body = new StatementVisitor(Context).Visit(context.stmt_assign());

            if (body is AssignStatement assign)
                Script.Blocks.Add(new AssignBlock(context.Span(), assign));
            else
                Errors.AddError("Assignment block must contain an assignment", body);

            return null;
        }

        public override object? VisitDecl_startup([NotNull] LogicScriptParser.Decl_startupContext context)
        {
            var body = new StatementVisitor(Context).Visit(context.block());

            Script.Blocks.Add(new StartupBlock(context.Span(), body));
            return null;
        }

        private void Visit(LogicScriptParser.Port_infoContext context, IDictionary<string, PortInfo> dic, MachinePorts target)
        {
            var size = context.size == null ? 1 : context.size.GetConstantValue(Context);

            if (size > BitsValue.BitSize)
                Errors.AddError($"The maximum bit size is {BitsValue.BitSize}", context.Span());

            var name = context.IDENT()?.GetText();
            if (name == null)
            {
                Errors.AddError("Missing register name", context.Span());
                return;
            }

            if (Script.Inputs.TryGetValue(name, out var port) || Script.Outputs.TryGetValue(name, out port) || Script.Registers.TryGetValue(name, out port))
            {
                Errors.AddError($"The name '{name}' is already taken by previous declaration at line {port.Span.Start.Line}", new SourceSpan(context.IDENT().Symbol));
                return;
            }

            int startIndex = dic.Values.Sum(o => o.BitSize);

            dic.Add(name, new PortInfo(target, startIndex, size, new(context.IDENT().Symbol)));
        }
    }
}
