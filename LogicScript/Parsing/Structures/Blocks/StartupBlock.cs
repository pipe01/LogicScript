using LogicScript.Parsing.Structures.Statements;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Blocks
{
    internal sealed class StartupBlock : Block
    {
        public Statement Body { get; set; }

        public StartupBlock(SourceSpan span, Statement body) : base(span)
        {
            this.Body = body;
        }

        protected override IEnumerator<ICodeNode> GetChildren()
        {
            yield return Body;
        }
    }
}
