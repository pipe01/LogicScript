using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;

namespace LogicScript.ByteCode
{
    partial struct Compiler
    {
        private void Visit(Statement stmt)
        {
            switch (stmt)
            {
                case AssignStatement assignStmt:
                    Visit(assignStmt);
                    break;
                
                case BlockStatement blockStmt:
                    Visit(blockStmt);
                    break;

                case BreakStatement breakStmt:
                    Visit(breakStmt);
                    break;

                case DeclareLocalStatement decLocalStmt:
                    Visit(decLocalStmt);
                    break;

                case IfStatement ifStmt:
                    Visit(ifStmt);
                    break;

                case ShowTaskStatement showTask:
                    Visit(showTask);
                    break;

                case WhileStatement whileStmt:
                    Visit(whileStmt);
                    break;

                default:
                    throw new Exception("Unknown statement structure");
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
            Push(OpCodes.Show);
        }

        private void Visit(IfStatement stmt)
        {
            Visit(stmt.Condition);

            var endLabel = NewLabel();
            Jump(OpCodes.Brz, endLabel);

            Visit(stmt.Body);

            Push(OpCodes.Nop);
            MarkLabel(endLabel);
        }

        private void Visit(WhileStatement stmt)
        {
            var endLabel = NewLabel(true);
            var startLabel = NewLabel(NextPosition);

            Visit(stmt.Condition);

            Jump(OpCodes.Brz, endLabel);

            Visit(stmt.Body);
            Jump(OpCodes.Jmp, startLabel);

            Push(OpCodes.Nop);
            MarkLabel(endLabel);
        }

        private void Visit(DeclareLocalStatement stmt)
        {
            if (stmt.Initializer != null)
                Visit(stmt.Initializer);
            else
                Push(OpCodes.Ld_0_1);

            Push(OpCodes.Stloc);
            Push(GetLocal(stmt.Local));
        }

        private void Visit(AssignStatement stmt)
        {
            if (stmt.Reference.Port is LocalInfo local)
            {
                if (!LocalsMap.TryGetValue(local.Name, out var index))
                    throw new Exception("Unknown local reference");

                Visit(stmt.Value);
                Push(OpCodes.Stloc);
                Push(index);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void Visit(BreakStatement stmt)
        {
            Jump(OpCodes.Jmp, LoopStack.Peek());
        }
    }
}