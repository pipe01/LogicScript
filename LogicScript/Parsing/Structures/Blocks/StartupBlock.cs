using LogicScript.Parsing.Structures.Statements;

namespace LogicScript.Parsing.Structures.Blocks
{
    internal sealed class StartupBlock : Block
    {
        public Statement Body { get; set; }

        public StartupBlock(SourceSpan span, Statement body) : base(span)
        {
            this.Body = body;
        }
    }
}
