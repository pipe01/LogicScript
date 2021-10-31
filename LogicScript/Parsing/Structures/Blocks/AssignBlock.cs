using LogicScript.Parsing.Structures.Statements;

namespace LogicScript.Parsing.Structures.Blocks
{
    internal class AssignBlock : Block
    {
        public AssignStatement Assignment { get; set; }

        public AssignBlock(SourceSpan span, AssignStatement assignment) : base(span)
        {
            this.Assignment = assignment;
        }
    }
}
