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
                case ShowTaskStatement showTask:
                    Visit(showTask);
                    break;
            }
        }

        private void Visit(ShowTaskStatement stmt)
        {
            Visit(stmt.Value);
            Push(OpCode.Show);
        }
    }
}