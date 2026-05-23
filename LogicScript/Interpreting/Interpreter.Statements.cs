using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Statements;
using System.Linq;

namespace LogicScript.Interpreting
{
    partial class Interpreter
    {
        /// <returns><see langword="true"/> if a <see langword="break"/> statement is found, <see langword="false"/> otherwise.</returns>
        private void ExecuteStatement(Statement stmt)
        {
            if (stmt is BlockStatement block)
                Visit(block);
            else if (stmt is AssignStatement assign)
                Visit(assign);
            else if (stmt is IfStatement ifStmt)
                Visit(ifStmt);
            else if (stmt is TaskStatement task)
                Visit(task);
            else if (stmt is DeclareLocalStatement local)
                Visit(local);
            else if (stmt is ForStatement forStmt)
                Visit(forStmt);
            else if (stmt is WhileStatement whileStmt)
                Visit(whileStmt);
            else if (stmt is BreakStatement)
                ClearToBreak();
            else
                throw new InterpreterException("Unknown statement", stmt.Span);
        }

        private void Visit(BlockStatement stmt)
        {
            OpStack.Push(new(null, after: () =>
            {
                foreach (var local in stmt.Locals)
                {
                    Locals.Remove(local.Value);
                }
            }));

            for (int i = stmt.Statements.Count - 1; i >= 0; i--)
            {
                OpStack.Push(new(stmt.Statements[i]));
            }

            OpStack.Push(new(null, after: () =>
            {
                foreach (var local in stmt.Locals)
                {
                    Locals.Add(local.Value, 0);
                }
            }));
        }

        private void Visit(AssignStatement stmt)
        {
            var value = Visit(stmt.Value);

            if (stmt.Reference is PortReference port)
            {
                switch (port.PortInfo.Target)
                {
                    case MachinePorts.Input:
                        throw new InterpreterException("Cannot write to input", stmt.Span);

                    case MachinePorts.Output:
                        if (value.Length > port.BitSize)
                            throw new InterpreterException("Value is longer than output", stmt.Span);

                        Machine!.WriteOutputs(port.StartIndex, new(value.Bits));
                        break;

                    case MachinePorts.Register:
                        Machine!.WriteRegister(port.StartIndex, value);
                        break;

                    default:
                        throw new InterpreterException("Unknown assignment target", stmt.Span);
                }
            }
            else if (stmt.Reference is LocalReference local)
            {
                Locals[local.LocalInfo] = value;
            }
            else
            {
                throw new InterpreterException("Unknown reference type", stmt.Span);
            }
        }

        private void Visit(IfStatement stmt)
        {
            var cond = Visit(stmt.Condition);

            if (cond.Number != 0)
                Push(stmt.Body);
            else if (stmt.Else != null)
                Push(stmt.Else);
        }

        private void Visit(TaskStatement stmt)
        {
            if (stmt is PrintTaskStatement print)
            {
                var locals = Locals;
                var values = print.String.Interpolations.Select(o => locals[o.Local].Number).Cast<object>().ToArray();

                Machine!.Print(string.Format(print.String.ToFormattable(), values));
            }
            else if (stmt is ShowTaskStatement show)
            {
                Machine!.Print(Visit(show.Value).ToString());
            }
            else if (stmt is UpdateTaskStatement)
            {
                Machine!.QueueUpdate();
            }
            else
            {
                throw new InterpreterException("Unknown task", stmt.Span);
            }
        }

        private void Visit(DeclareLocalStatement stmt)
        {
            var value = stmt.Initializer == null ? new BitsValue(0, stmt.Local.BitSize) : Visit(stmt.Initializer);

            Locals[stmt.Local] = value;
        }

        private void Visit(ForStatement stmt)
        {
            var from = stmt.From == null ? 0 : Visit(stmt.From);
            var to = Visit(stmt.To);

            int size = to.Length;

            OpStack.Push(new(null, breakBarrier: true));

            for (ulong i = from; i < to.Number; i++)
            {
                OpStack.Push(new(stmt.Body, before: () =>
                {
                    Locals[stmt.Variable] = new BitsValue(i, size);
                    return true;
                }));
            }
        }

        private void Visit(WhileStatement stmt)
        {
            OpStack.Push(new(null, breakBarrier: true));

            Operation loopOp = default;
            loopOp = new Operation(null, before: () =>
            {
                if (Visit(stmt.Condition).Number != 0)
                {
                    OpStack.Push(loopOp);
                    Push(stmt.Body);
                    return true;
                }

                return false;
            });

            OpStack.Push(loopOp);
        }
    }
}
