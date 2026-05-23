using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class DeclareLocalStatement(SourceSpan span, LocalInfo local, Expression? initializer, bool hasExplicitSize) : Statement(span)
    {
        public LocalInfo Local { get; } = local;
        public Expression? Initializer { get; } = initializer;
        public bool HasExplicitSize { get; } = hasExplicitSize;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Local;
            if (Initializer != null)
                yield return Initializer;
        }
    }
}
