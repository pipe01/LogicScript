using LogicScript.Parsing.Structures.Statements;

namespace LogicScript.Parsing.Structures
{
    internal class WhenBlock : ICodeNode
    {
        public SourceLocation Location { get; }
        public Statement Body { get; }

        public WhenBlock(SourceLocation location, Statement body)
        {
            this.Location = location;
            this.Body = body;
        }
    }
}
