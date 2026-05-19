using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal class BlockStatement : Statement
    {
        public IReadOnlyList<Statement> Statements { get; set; }
        public IDictionary<string, LocalInfo> Locals { get; }

        public BlockStatement(SourceSpan span, IReadOnlyList<Statement> statements, IDictionary<string, LocalInfo> locals) : base(span)
        {
            this.Statements = statements;
            this.Locals = locals;
        }

        public override IEnumerable<ICodeNode> GetChildren()
        {
            foreach (var item in Statements)
            {
                yield return item;
            }
        }
    }
}
