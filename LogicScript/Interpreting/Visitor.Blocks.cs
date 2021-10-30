using LogicScript.Parsing.Structures.Blocks;

namespace LogicScript.Interpreting
{
    partial struct Visitor
    {
        public void Visit(Block block)
        {
            switch (block)
            {
                case WhenBlock @when:
                    Visit(@when);
                    break;
                case AssignBlock assign:
                    Visit(assign);
                    break;
                case StartupBlock startup:
                    Visit(startup);
                    break;
                default:
                    throw new InterpreterException("Unknown block type", block.Location);
            }
        }

        public void Visit(WhenBlock block)
        {
            if (block.Condition == null || Visit(block.Condition).Number != 0)
                Visit(block.Body);
        }

        public void Visit(AssignBlock block)
        {
            Visit(block.Assignment);
        }

        public void Visit(StartupBlock block)
        {
            Visit(block.Body);
        }
    }
}
