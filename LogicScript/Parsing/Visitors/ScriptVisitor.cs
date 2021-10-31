using Antlr4.Runtime.Misc;

namespace LogicScript.Parsing.Visitors
{
    internal class ScriptVisitor : LogicScriptBaseVisitor<Script>
    {
        private readonly ErrorSink Errors;

        public ScriptVisitor(ErrorSink errors)
        {
            this.Errors = errors;
        }

        public override Script VisitScript([NotNull] LogicScriptParser.ScriptContext context)
        {
            var script = new Script();
            var ctx = new ScriptContext(script, Errors);
            var visitor = new DeclarationVisitor(ctx, Errors);

            foreach (var decl in context.declaration())
            {
                visitor.Visit(decl);
            }

            return script;
        }
    }
}
