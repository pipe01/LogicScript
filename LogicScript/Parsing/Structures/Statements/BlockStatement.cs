using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal class BlockStatement : Statement
    {
        public IReadOnlyList<Statement> Statements { get; set; }

        public BlockStatement(SourceSpan span, IReadOnlyList<Statement> statements) : base(span)
        {
            this.Statements = statements;
        }

        protected override IEnumerator<ICodeNode> GetChildren()
        {
            foreach (var item in Statements)
            {
                yield return item;
            }
        }
    }
}
