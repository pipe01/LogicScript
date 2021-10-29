using LogicScript.Parsing.Structures.Blocks;

namespace LogicScript.Interpreting
{
    partial struct Visitor
    {
        public void Visit(Block block)
        {
            if (block is WhenBlock when)
                Visit(when);
            else if (block is AssignBlock assign)
                Visit(assign);
            else
                throw new InterpreterException("Unknown block type", block.Location);
        }

        public void Visit(WhenBlock block)
        {
            if (block.Condition != null && Visit(block.Condition).Number == 0)
                return;

            Visit(block.Body);
        }

        public void Visit(AssignBlock block)
        {
            Visit(block.Assignment);
        }
    }
}
