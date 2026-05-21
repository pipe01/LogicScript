using LogicScript.Parsing.Structures.Statements;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Blocks
{
    internal sealed class StartupBlock(SourceSpan span, Statement body) : Block(span)
    {
        public Statement Body { get; set; } = body;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Body;
        }
    }
}
