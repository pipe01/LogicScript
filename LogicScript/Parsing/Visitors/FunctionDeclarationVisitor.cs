using Antlr4.Runtime.Misc;
using LogicScript.Parsing.Structures;
using System.Collections.Generic;

namespace LogicScript.Parsing.Visitors
{
    internal class FunctionDeclarationVisitor(ScriptContext scriptContext) : LogicScriptParserBaseVisitor<object?>
    {
        public override object? VisitDecl_function([NotNull] LogicScriptParser.Decl_functionContext context)
        {
            var name = context.name.Text;
            var nameSpan = context.name.Span();
            var resultSize = scriptContext.ParseBitSize(context.ret_size);
            var parameters = context.param_list() == null ? [] : ParseParameters(scriptContext, context.param_list());

            if (!scriptContext.Script.Functions.TryAdd(name, new(NodeID.Next(), context.Span(), name, nameSpan, resultSize, [.. parameters], null)))
                scriptContext.Errors.AddFunctionAlreadyDefined(name, nameSpan);

            return null;
        }

        public static IEnumerable<LocalInfo> ParseParameters(ScriptContext scriptContext, LogicScriptParser.Param_listContext context)
        {
            var name = context.name.Text;
            var size = context.size == null ? 0 : scriptContext.ParseBitSize(context.size);

            yield return new(NodeID.Next(), size, name, context.name.Span());

            if (context.param_list() != null)
            {
                foreach (var param in ParseParameters(scriptContext, context.param_list()))
                {
                    yield return param;
                }
            }
        }
    }
}