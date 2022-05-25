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
            Push(OpCode.Show);
        }

        private void Visit(IfStatement stmt)
        {
            Visit(stmt.Condition);

            var endLabel = NewLabel();
            Jump(OpCode.Brz, endLabel);

            Visit(stmt.Body);

            Push(OpCode.Nop);
            MarkLabel(endLabel);
        }

        private void Visit(DeclareLocalStatement stmt)
        {
            if (stmt.Initializer != null)
                Visit(stmt.Initializer);
            else
                Push(OpCode.Ld_0_1);

            Push(OpCode.Stloc);
            Push(GetLocal(stmt.Local));
        }

        private void Visit(AssignStatement stmt)
        {
            if (stmt.Reference.Port is LocalInfo local)
            {
                if (!LocalsMap.TryGetValue(local.Name, out var index))
                    throw new Exception("Unknown local reference");

                Visit(stmt.Value);
                Push(OpCode.Stloc);
                Push(index);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void Visit(WhileStatement stmt)
        {
            var endLabel = NewLabel(true);
            var startPos = CurrentPosition;

            Visit(stmt.Condition);
            Jump(OpCode.Brz, endLabel);

            Visit(stmt.Body);
            Jump(OpCode.Jmp, startPos);

            Push(OpCode.Nop);
            MarkLabel(endLabel);
        }

        private void Visit(BreakStatement stmt)
        {
            Jump(OpCode.Jmp, LoopStack.Peek());
        }
    }
}