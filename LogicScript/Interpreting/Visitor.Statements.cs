﻿using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Statements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
            else if (stmt is DeclareLocalStatement local)
                Visit(local);
            else if (stmt is ForStatement forStmt)
                Visit(forStmt);
            else
                throw new InterpreterException("Unknown statement", stmt.Span);
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

            if (stmt.Reference is PortReference port)
            {
                switch (port.Target)
                {
                    case ReferenceTarget.Input:
                        throw new InterpreterException("Cannot write to input", stmt.Span);

                    case ReferenceTarget.Output:
                        if (value.Length > port.BitSize)
                            throw new InterpreterException("Value is longer than output", stmt.Span);

                        Machine.WriteOutput(port.StartIndex, value.Bits);
                        break;

                    case ReferenceTarget.Register:
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
                Machine.Print(FormatString(print.Text, Locals));
            else if (stmt is ShowTaskStatement show)
                Machine.Print(Visit(show.Value).ToString());
            else
                throw new InterpreterException("Unknown task", stmt.Span);

            string FormatString(string str, IDictionary<string, BitsValue> locals)
            {
                return Regex.Replace(str, @"\$([a-zA-Z_][a-zA-Z0-9_]*)(:(?<base>b|x))?", m =>
                {
                    if (!locals.TryGetValue(m.Groups[1].Value, out var value))
                        throw new InterpreterException($"Local variable ${m.Value} not found in string interpolation", stmt.Span);

                    var nBase = m.Groups["base"].Success ? m.Groups["base"].Value : null;

                    return nBase == "x" ? value.ToStringHex()
                        : nBase == "b" ? value.ToStringBinary()
                        : value.ToString();
                });
            }
        }

        private void Visit(DeclareLocalStatement stmt)
        {
            var value = stmt.Initializer == null ? new BitsValue(0, stmt.Size) : Visit(stmt.Initializer);

            Locals.Add(stmt.Name, value);
        }

        private void Visit(ForStatement stmt)
        {
            var from = stmt.From == null ? 0 : Visit(stmt.From);
            var to = Visit(stmt.To);

            if (from > to)
            {
                int size = from.Length;

                for (ulong i = from; i > to.Number; i--)
                {
                    Locals[stmt.VariableName] = new BitsValue(i, size);

                    Visit(stmt.Body);
                }
            }
            else
            {
                int size = to.Length;

                for (ulong i = from; i < to.Number; i++)
                {
                    Locals[stmt.VariableName] = new BitsValue(i, size);

                    Visit(stmt.Body);
                }
            }
        }
    }
}
