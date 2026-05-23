using LogicScript.Parsing.Structures.Blocks;

namespace LogicScript.Interpreting
{
    partial class Interpreter
    {
        private void Visit(Block block)
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
                    throw new InterpreterException("Unknown block type", block.Span.Start);
            }
        }

        private void Visit(WhenBlock block)
        {
            if (block.Condition == null || Visit(block.Condition).Number != 0)
                Push(block.Body);
        }

        private void Visit(AssignBlock block)
        {
            Push(block.Assignment);
        }

        private void Visit(StartupBlock block)
        {
            Push(block.Body);
        }
    }
}
