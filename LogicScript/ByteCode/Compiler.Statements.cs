using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;

namespace LogicScript.ByteCode
{
    partial struct Compiler
    {
        private void Visit(Statement expr)
        {
            switch (expr)
            {
                case BlockStatement blockStmt:
                    Visit(blockStmt);
                    break;

                case IfStatement ifStmt:
                    Visit(ifStmt);
                    break;

                case ShowTaskStatement showTask:
                    Visit(showTask);
                    break;
            }
        }

        private void Visit(BlockStatement stmt)
        {
            foreach (var item in stmt.Statements)
            {
                Visit(item);
            }
        }

        private void Visit(ShowTaskStatement stmt)
        {
            Visit(stmt.Value);
            Push(OpCode.Show);
        }

        private void Visit(IfStatement stmt)
        {
            Visit(stmt.Condition);

            var endLabel = NewLabel();
            Jump(OpCode.Brz, ref endLabel);

            Visit(stmt.Body);

            Push(OpCode.Nop);
            MarkLabel(endLabel);
        }
    }
}