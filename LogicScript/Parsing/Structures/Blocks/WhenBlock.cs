using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Blocks
{
    internal class WhenBlock : Block
    {
        /// <summary>
        /// If null, the block will be run unconditionally.
        /// </summary>
        public Expression? Condition { get; }
        public Statement Body { get; }

        public IDictionary<string, PortInfo> Locals { get; } = new Dictionary<string, PortInfo>();

        public WhenBlock(SourceLocation location, Expression? condition, Statement body) : base(location)
        {
            this.Body = body;
            this.Condition = condition;
        }
    }
}
