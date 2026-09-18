using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal class BlockStatement(NodeID id, SourceSpan span, IReadOnlyList<Statement> statements, IReadOnlyCollection<LocalInfo> locals) : Statement(id, span)
    {
        public IReadOnlyList<Statement> Statements { get; set; } = statements;
        public IReadOnlyCollection<LocalInfo> Locals { get; } = locals;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            foreach (var item in Statements)
            {
                yield return item;
            }
        }
    }
}
