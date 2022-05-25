using System;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.ByteCode
{
    partial struct Compiler
    {
        private void Visit(Block block)
        {
            switch (block)
            {
                case AssignBlock assBlock:
                    Visit(assBlock.Assignment);
                    break;

                case StartupBlock startBlock:
                    Visit(startBlock.Body);
                    break;

                case WhenBlock whenBlock:
                    Visit(whenBlock);
                    break;

                default:
                    throw new Exception("Unknown block structure");
            }
        }

        private void Visit(WhenBlock block)
        {
            if (block.Condition == null)
            {
                Visit(block.Body);
                return;
            }

            if (block.Condition is NumberLiteralExpression lit)
            {
                if (lit.Value.Number != 0)
                    Visit(block.Body);

                return;
            }

            Visit(block.Condition);

            var endLabel = NewLabel();
            Jump(OpCode.Brz, endLabel);

            Visit(block.Body);

            MarkLabel(endLabel);
        }
    }
}