using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Statements;
using LogicScript.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LogicScript.Interpreting
{
    partial struct Visitor
    {
        /// <returns><see langword="true"/> if a <see langword="break"/> statement is found, <see langword="false"/> otherwise.</returns>
        public bool Visit(Statement stmt)
        {
            if (stmt is BlockStatement block)
                return Visit(block);
            else if (stmt is AssignStatement assign)
                return Visit(assign);
            else if (stmt is IfStatement ifStmt)
                return Visit(ifStmt);
            else if (stmt is TaskStatement task)
                return Visit(task);
            else if (stmt is DeclareLocalStatement local)
                return Visit(local);
            else if (stmt is ForStatement forStmt)
                return Visit(forStmt);
            else if (stmt is WhileStatement whileStmt)
                return Visit(whileStmt);
            else if (stmt is BreakStatement)
                return true;
            else
                throw new InterpreterException("Unknown statement", stmt.Span);
        }

        private bool Visit(BlockStatement stmt)
        {
            foreach (var item in stmt.Statements)
            {
                if (Visit(item))
                    return true;
            }

            return false;
        }

        private bool Visit(AssignStatement stmt)
        {
            AssertNotConstant(stmt);

            var value = Visit(stmt.Value);

            if (stmt.Truncate != null)
                value = value.Resize(stmt.Truncate.Value);

            if (stmt.Reference is PortReference port)
            {
                switch (port.PortInfo.Target)
                {
                    case MachinePorts.Input:
                        throw new InterpreterException("Cannot write to input", stmt.Span);

                    case MachinePorts.Output:
                        if (value.Length > port.BitSize)
                            throw new InterpreterException("Value is longer than output", stmt.Span);

                        Machine.WriteOutput(port.StartIndex, value.Bits);
                        break;

                    case MachinePorts.Register:
                        Machine.WriteRegister(port.StartIndex, value);
                        break;

                    default:
                        throw new InterpreterException("Unknown assignment target", stmt.Span);
                }
            }
            else if (stmt.Reference is LocalReference local)
            {
                Locals[local.Name] = value;
            }
            else
            {
                throw new InterpreterException("Unknown reference type", stmt.Span);
            }

            return false;
        }

        private bool Visit(IfStatement stmt)
        {
            var cond = Visit(stmt.Condition);

            if (cond.Number != 0)
                return Visit(stmt.Body);
            else if (stmt.Else != null)
                return Visit(stmt.Else);

            return false;
        }

        private bool Visit(TaskStatement stmt)
        {
            AssertNotConstant(stmt);

            if (stmt is PrintTaskStatement print)
            {
                Machine.Print(PrintStringFormat.Format(print.Text, Locals));
            }
            else if (stmt is ShowTaskStatement show)
            {
                Machine.Print(Visit(show.Value).ToString());
            }
            else if (stmt is UpdateTaskStatement)
            {
                if (Machine is not IUpdatableMachine upd)
                    throw new InterpreterException("This machine cannot queue updates", stmt.Span);

                upd.QueueUpdate();
            }
            else
            {
                throw new InterpreterException("Unknown task", stmt.Span);
            }

            return false;
        }

        private bool Visit(DeclareLocalStatement stmt)
        {
            var value = stmt.Initializer == null ? new BitsValue(0, stmt.Local.BitSize) : Visit(stmt.Initializer);

            Locals.Add(stmt.Local.Name, value);

            return false;
        }

        private bool Visit(ForStatement stmt)
        {
            var from = stmt.From == null ? 0 : Visit(stmt.From);
            var to = Visit(stmt.To);

            if (from > to)
            {
                int size = from.Length;

                for (ulong i = from; i > to.Number; i--)
                {
                    Locals[stmt.VariableName] = new BitsValue(i, size);

                    if (Visit(stmt.Body))
                        break;
                }
            }
            else
            {
                int size = to.Length;

                for (ulong i = from; i < to.Number; i++)
                {
                    Locals[stmt.VariableName] = new BitsValue(i, size);

                    if (Visit(stmt.Body))
                        break;
                }
            }

            return false;
        }

        private bool Visit(WhileStatement stmt)
        {
            while (Visit(stmt.Condition).Number != 0)
            {
                if (Visit(stmt.Body))
                    break;
            }

            return false;
        }
    }
}
