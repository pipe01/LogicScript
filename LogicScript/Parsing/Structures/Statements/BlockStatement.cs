using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal class BlockStatement(SourceSpan span, IReadOnlyList<Statement> statements, IDictionary<string, LocalInfo> locals) : Statement(span)
    {
        public IReadOnlyList<Statement> Statements { get; set; } = statements;
        public IDictionary<string, LocalInfo> Locals { get; } = locals;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            foreach (var item in Statements)
            {
                yield return item;
            }
        }
    }
}
