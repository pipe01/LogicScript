using Antlr4.Runtime.Misc;

namespace LogicScript.Parsing.Visitors
{
    internal class ScriptVisitor(ErrorSink errors) : LogicScriptBaseVisitor<Script>
    {
        public override Script VisitScript([NotNull] LogicScriptParser.ScriptContext context)
        {
            var script = new Script(context.Start.TokenSource.SourceName, errors);
            var ctx = new ScriptContext(script, errors);
            var visitor = new DeclarationVisitor(ctx, errors);

            foreach (var decl in context.declaration())
            {
                visitor.Visit(decl);
            }

            return script;
        }
    }
}
