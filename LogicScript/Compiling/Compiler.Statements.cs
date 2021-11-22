using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogicScript.Data;
using LogicScript.Parsing.Structures.Statements;

namespace LogicScript.Compiling
{
    partial struct Compiler
    {
        private void Visit(Statement stmt)
        {
            switch (stmt)
            {
                case BlockStatement block:
                    Visit(block);
                    break;

                case TaskStatement task:
                    Visit(task);
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

        private void Visit(TaskStatement stmt)
        {
            switch (stmt)
            {
                case ShowTaskStatement show:
                    LoadMachine();
                    Visit(show.Value);
                    IL.Box(typeof(BitsValue));
                    IL.Call(typeof(object).GetMethod(nameof(object.ToString)));
                    IL.Call(typeof(IMachine).GetMethod(nameof(IMachine.Print)));
                    break;
                case PrintTaskStatement print:
                    LoadMachine();
                    IL.Ldstr(print.Text);
                    IL.Call(typeof(IMachine).GetMethod(nameof(IMachine.Print)));
                    break;
            }
        }
    }
}