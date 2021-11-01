using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class DeclareLocalStatement : Statement
    {
        public LocalInfo Local { get; }
        public Expression? Initializer { get; }

        public DeclareLocalStatement(SourceSpan span, LocalInfo local, Expression? initializer) : base(span)
        {
            this.Local = local;
            this.Initializer = initializer;
        }

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Local;
            if (Initializer != null)
                yield return Initializer;
        }
    }
}
