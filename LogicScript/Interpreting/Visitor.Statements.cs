using LogicScript.Parsing.Structures;
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

        private void Visit(AssignStatement stmt)
        {
            var value = Visit(stmt.Value);

            switch (stmt.Reference.Target)
            {
                case ReferenceTarget.Input:
                    throw new InterpreterException("Cannot write to input", stmt.Location);

                case ReferenceTarget.Output:
                    if (value.Length > stmt.Reference.Length)
                        throw new InterpreterException("Value is longer than output", stmt.Location);

                    Machine.WriteOutput(stmt.Reference.StartIndex, value);
                    break;

                case ReferenceTarget.Register:
                    Machine.WriteRegister(stmt.Reference.StartIndex, value);
                    break;

                default:
                    throw new InterpreterException("Unknown assignment target", stmt.Location);
            }
        }

        private void Visit(IfStatement stmt)
        {
            var cond = Visit(stmt.Condition);

            if (cond.Number != 0)
                Visit(stmt.Body);
            else if (stmt.Else != null)
                Visit(stmt.Else);
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
