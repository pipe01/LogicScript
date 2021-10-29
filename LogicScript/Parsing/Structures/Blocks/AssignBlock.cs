using LogicScript.Parsing.Structures.Statements;

namespace LogicScript.Parsing.Structures.Blocks
{
    internal class AssignBlock : Block
    {
        public AssignStatement Assignment { get; set; }

        public AssignBlock(SourceLocation location, AssignStatement assignment) : base(location)
        {
            this.Assignment = assignment;
        }
    }
}
