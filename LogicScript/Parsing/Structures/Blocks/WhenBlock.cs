using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Blocks
{
    internal class WhenBlock(SourceSpan span, Expression? condition, Statement body) : Block(span)
    {
        /// <summary>
        /// If null, the block will be run unconditionally.
        /// </summary>
        public Expression? Condition { get; } = condition;
        public Statement Body { get; } = body;

        public IDictionary<string, MachinePortInfo> Locals { get; } = new Dictionary<string, MachinePortInfo>();

        public override IEnumerable<ICodeNode> GetChildren()
        {
            if (Condition != null)
                yield return Condition;
            yield return Body;
        }
    }
}
