using LogicScript.Parsing.Structures.Statements;

namespace LogicScript.Parsing.Structures.Blocks
{
    internal sealed class StartupBlock : Block
    {
        public Statement Body { get; set; }

        public StartupBlock(SourceLocation location, Statement body) : base(location)
        {
            this.Body = body;
        }
    }
}
