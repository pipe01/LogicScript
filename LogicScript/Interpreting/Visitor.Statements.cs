using LogicScript.Parsing.Structures.Statements;

namespace LogicScript.Interpreting
{
    partial struct Visitor
    {
        public void Visit(Statement stmt)
        {
            if (stmt is BlockStatement block)
                Visit(block);
            else if (stmt is AssignStatement assign)
                Visit(assign);
            else if (stmt is IfStatement ifStmt)
                Visit(ifStmt);
            else if (stmt is TaskStatement task)
                Visit(task);
            else
                throw new InterpreterException("Unknown statement", stmt.Location);
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
            if (stmt is PrintTaskStatement print)
                Machine.Print(print.Text);
            else if (stmt is ShowTaskStatement show)
                Machine.Print(Visit(show.Value).ToString());
            else
                throw new InterpreterException("Unknown task", stmt.Location);
        }
    }
}
