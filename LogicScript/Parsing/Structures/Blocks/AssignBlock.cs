using LogicScript.Parsing.Structures.Statements;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Blocks
{
    internal class AssignBlock(SourceSpan span, AssignStatement assignment) : Block(span)
    {
        public AssignStatement Assignment { get; set; } = assignment;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Assignment;
        }
    }
}
