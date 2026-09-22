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
                    Errors.AddDuplicateConstant(name, prevLine, nameSpan);
                }
            }
            else
            {
                if (value is not PlaceholderExpression)
                    Errors.AddConstValueRequired(value);

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
                Context.Errors.AddWhenConditionMissing(context.Span());

                cond = new PlaceholderExpression(new(context.space.Span().Start, context.space.Span().End));
            }

            var body = context.block() == null
                ? new BlockStatement(NodeID.Next(), context.Span(), [], [])
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
                Errors.AddAssignmentRequired(context.Span());
            }

            return null;
        }

        public override object? VisitDecl_startup([NotNull] LogicScriptParser.Decl_startupContext context)
        {
            var body = new StatementVisitor(Context).Visit(context.block());

            Script.Blocks.Add(new StartupBlock(context.Span(), body));
            return null;
        }

        public override object? VisitDecl_function([NotNull] LogicScriptParser.Decl_functionContext context)
        {
            var name = context.name.Text;
            var declaration = Script.Functions[name];

            var blockContext = new BlockContext(Context, functionResultSize: declaration.ResultSize);
            blockContext.Locals.AddRange(declaration.Parameters);

            var body = context.block() == null
                ? new BlockStatement(NodeID.Next(), new(), [], [])
                : (BlockStatement)new StatementVisitor(Context, blockContext).Visit(context.block());

            if (!body.Statements.OfType<ReturnStatement>().Any()) // TODO: replace with control flow analysis lol
                Context.Errors.AddFunctionReturnRequired(context.end.Span());

            // Replace empty function declaration with a definition that contains the body
            Script.Functions[name] = new(NodeID.Next(), context.Span(), name, context.name.Span(), declaration.ResultSize, declaration.Parameters, body);

            return null;
        }

        private void Visit(LogicScriptParser.Port_infoContext context, IDictionary<string, MachinePortInfo> dic, MachinePorts target)
        {
            int size = context.size == null ? 1 : Context.ParseBitSize(context.size);

            int length = 1;
            Expression? lengthExpression = null;

            if (context.simple_indexer()?.index != null)
            {
                lengthExpression = new ExpressionVisitor(new(Context, isInConstant: true)).Visit(context.simple_indexer().index);

                if (lengthExpression is not PlaceholderExpression)
                    length = (int)lengthExpression.GetConstantValue().Number;
            }

            if (length <= 0)
            {
                Errors.AddVectorLengthGreaterZero(context.Span());
                length = 1;
            }

            var name = context.IDENT()?.GetText();
            if (name == null)
            {
                Errors.AddPortNameMissing(context.Span());
                return;
            }

            if (Script.Inputs.TryGetValue(name, out var port) || Script.Outputs.TryGetValue(name, out port) || Script.Registers.TryGetValue(name, out port))
            {
                Errors.AddDuplicateDeclaration(name, port.Span.Start.Line, new SourceSpan(context.IDENT().Symbol));
                return;
            }
            if (Script.Constants.TryGetValue(name, out var @const))
            {
                Errors.AddDuplicateConstant(name, @const.Expression.Span.Start.Line, new SourceSpan(context.IDENT().Symbol));
                return;
            }

            // See MachinePortInfo.StartIndex
            int startIndex = target == MachinePorts.Register ? dic.Count : dic.Values.Sum(o => o.BitSize);

            dic.Add(name, new MachinePortInfo(name, target, startIndex, size, length, lengthExpression, new(context.IDENT().Symbol)));
        }
    }
}
