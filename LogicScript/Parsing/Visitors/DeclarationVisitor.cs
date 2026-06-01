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
    internal class DeclarationVisitor(ScriptContext context, ErrorSink errors) : LogicScriptParserBaseVisitor<object?>
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
            var value = new ExpressionVisitor(new BlockContext(Context, null, true)).VisitOrPlaceholder(context.expression(), context.Span());
            var name = context.IDENT().GetText();
            var nameSpan = context.IDENT().Symbol.Span();

            if (value.IsConstant)
            {
                if (!Context.Script.Constants.TryAdd(name, new(value.GetConstantValue(), value, nameSpan)))
                {
                    var prevLine = Context.Script.Constants[name].Expression.Span.Start.Line;
                    Errors.AddError($"The name '{name}' is already taken by previous constant at line {prevLine}", nameSpan);
                }
            }
            else
            {
                if (value is not PlaceholderExpression)
                    Errors.AddError("Const declarations must have a constant value", value);

                Context.Script.Constants.Add(name, new(0, new PlaceholderExpression(context.Span()), nameSpan));
            }

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
            var body = context.stmt_assign() == null ? null : new StatementVisitor(Context).Visit(context.stmt_assign());

            if (body is AssignStatement assign)
            {
                Script.Blocks.Add(new AssignBlock(context.Span(), assign));
            }
            else
            {
                Script.Blocks.Add(new PlaceholderAssignBlock(context.Span()));
                Errors.AddError("Assignment block must contain an assignment", context.Span());
            }

            return null;
        }

        public override object? VisitDecl_startup([NotNull] LogicScriptParser.Decl_startupContext context)
        {
            var body = new StatementVisitor(Context).Visit(context.block());

            Script.Blocks.Add(new StartupBlock(context.Span(), body));
            return null;
        }

        private void Visit(LogicScriptParser.Port_infoContext context, IDictionary<string, MachinePortInfo> dic, MachinePorts target)
        {
            int size = context.size == null ? 1 : (int)context.size.GetConstantValue(Context);

            if (size > BitsValue.BitSize)
                Errors.AddError($"The maximum bit size is {BitsValue.BitSize}", context.Span());

            int length = 1;

            if (context.simple_indexer()?.index != null)
            {
                var indexExpr = new ExpressionVisitor(new(Context, isInConstant: true)).Visit(context.simple_indexer().index);

                if (indexExpr is not PlaceholderExpression)
                    length = (int)indexExpr.GetConstantValue().Number;
            }

            if (length <= 0)
            {
                Errors.AddError("Vectors length must be greater than zero", context.Span());
                length = 1;
            }

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
            if (Script.Constants.TryGetValue(name, out var @const))
            {
                Errors.AddError($"The name '{name}' is already taken by previous constant at line {@const.Expression.Span.Start.Line}", new SourceSpan(context.IDENT().Symbol));
                return;
            }

            int startIndex = dic.Values.Sum(o => o.BitSize);

            dic.Add(name, new MachinePortInfo(target, startIndex, size, length, new(context.IDENT().Symbol)));
        }
    }
}
