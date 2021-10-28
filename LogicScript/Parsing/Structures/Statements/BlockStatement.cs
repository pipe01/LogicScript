using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal class BlockStatement : Statement
    {
        public IReadOnlyList<Statement> Statements { get; set; }

        public BlockStatement(SourceLocation location, IReadOnlyList<Statement> statements) : base(location)
        {
            this.Statements = statements;
        }
    }
}
